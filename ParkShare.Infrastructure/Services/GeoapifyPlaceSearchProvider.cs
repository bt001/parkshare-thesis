using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Configuration;
namespace ParkShare.Infrastructure.Services
{
    /// <summary>Bounded, cached Geoapify lookups for a single API process.</summary>
    public sealed class GeoapifyPlaceSearchProvider : IPlaceSearchProvider, IDisposable
    {
        private readonly HttpClient _client;
        private readonly IOptionsMonitor<PlaceSearchConfig> _options;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly Dictionary<string, (DateTimeOffset Expires, IReadOnlyList<PlaceResponse> Items)> _cache = new();
        private DateTimeOffset _nextRequest;
        private DateOnly _day;
        private int _requests;
        /// <summary>Creates a reusable client without HTTP logging of credential-bearing URLs.</summary>
        public GeoapifyPlaceSearchProvider(IOptionsMonitor<PlaceSearchConfig> options)
            : this(options, new HttpClient { Timeout = TimeSpan.FromSeconds(8) }) { }
        /// <summary>Creates the adapter with a supplied HTTP client for transport verification.</summary>
        public GeoapifyPlaceSearchProvider(IOptionsMonitor<PlaceSearchConfig> options, HttpClient client)
        { _options = options; _client = client; }
        /// <inheritdoc/>
        public Task<IReadOnlyList<PlaceResponse>> SearchAsync(string text, CancellationToken ct)
            => LookupAsync("place", text, null, null, null, ct);
        /// <inheritdoc/>
        public Task<IReadOnlyList<PlaceResponse>> SearchStreetsAsync(string text, CancellationToken ct)
            => LookupAsync("street", text, null, null, null, ct);
        /// <inheritdoc/>
        public Task<IReadOnlyList<PlaceResponse>> ResolveAddressAsync(string street, string houseNumber, string? city, string? postcode, CancellationToken ct)
            => LookupAsync("address", street, houseNumber, city, postcode, ct);
        /// <summary>Shares cache, quota and transport coordination across lookup operations.</summary>
        private async Task<IReadOnlyList<PlaceResponse>> LookupAsync(string operation, string text, string? houseNumber, string? city, string? postcode, CancellationToken ct)
        {
            var config = _options.CurrentValue;
            if (string.IsNullOrWhiteSpace(config.ApiKey)) throw new InvalidOperationException("Place search is not configured.");
            if (!await _gate.WaitAsync(TimeSpan.Zero, ct)) throw new InvalidOperationException("Place search is busy.");
            try
            {
                var cacheKey = JsonSerializer.Serialize(new[] { config.CountryCode, config.Language, operation, text.ToUpperInvariant(), houseNumber?.ToUpperInvariant(), city?.ToUpperInvariant(), postcode?.ToUpperInvariant() });
                var now = DateTimeOffset.UtcNow;
                if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expires > now) return cached.Items;
                var results = await RequestAsync(text, config, operation == "street" ? "street" : null, config.Language, ct, houseNumber, city, postcode);
                // Some English city names resolve only to businesses in the general index.
                if (operation == "place" && results.Length > 0 && results.All(item => item.TryGetProperty("result_type", out var type) && type.GetString() == "amenity")
                    && !text.Any(char.IsDigit))
                {
                    var language = text.Any(c => c >= '\u0370' && c <= '\u03ff') ? "el" : "en";
                    var cities = await RequestAsync(text, config, "city", language, ct);
                    var exactCities = cities.Where(item => item.TryGetProperty("result_type", out var type) && type.GetString() == "city"
                        && item.TryGetProperty("name", out var name) && string.Equals(name.GetString(), text, StringComparison.OrdinalIgnoreCase));
                    results = exactCities.Concat(results).Take(5).ToArray();
                }
                var items = new List<PlaceResponse>();
                foreach (var item in results.Take(5))
                {
                    if (!item.TryGetProperty("lat", out var lat) || lat.ValueKind != JsonValueKind.Number || !lat.TryGetDouble(out var latitude)
                        || !item.TryGetProperty("lon", out var lon) || lon.ValueKind != JsonValueKind.Number || !lon.TryGetDouble(out var longitude)) continue;
                    var label = ReadString(item, "formatted");
                    var street = ReadString(item, "street");
                    if (operation == "street" && string.IsNullOrWhiteSpace(street)) continue;
                    if (double.IsFinite(latitude) && double.IsFinite(longitude) && latitude is >= -90 and <= 90
                        && longitude is >= -180 and <= 180 && !string.IsNullOrWhiteSpace(label))
                        items.Add(new() { Label = label, Latitude = latitude, Longitude = longitude,
                            Street = street, HouseNumber = ReadString(item, "housenumber"),
                            City = ReadString(item, "city") ?? ReadString(item, "town") ?? ReadString(item, "village") ?? ReadString(item, "municipality"),
                            Postcode = ReadString(item, "postcode") });
                }
                if (_cache.Count >= 200) _cache.Clear();
                _cache[cacheKey] = (DateTimeOffset.UtcNow.AddHours(1), items);
                return items;
            }
            finally { _gate.Release(); }
        }
        /// <summary>Issues one counted, spaced provider request; all calls share the process gate.</summary>
        private async Task<JsonElement[]> RequestAsync(string text, PlaceSearchConfig config, string? type, string language, CancellationToken ct, string? houseNumber = null, string? city = null, string? postcode = null)
        {
            var now = DateTimeOffset.UtcNow;
            var day = DateOnly.FromDateTime(now.UtcDateTime);
            if (_day != day) { _day = day; _requests = 0; }
            if (_requests >= Math.Clamp(config.DailyRequestLimit, 0, 2500)) throw new InvalidOperationException("Place search allowance reached.");
            if (_nextRequest > now) await Task.Delay(_nextRequest - now, ct);
            _requests++;
            _nextRequest = DateTimeOffset.UtcNow.AddMilliseconds(300);
            var query = houseNumber is null ? "&text=" + Uri.EscapeDataString(text)
                : "&street=" + Uri.EscapeDataString(text) + "&housenumber=" + Uri.EscapeDataString(houseNumber)
                    + (string.IsNullOrWhiteSpace(city) ? "" : "&city=" + Uri.EscapeDataString(city))
                    + (string.IsNullOrWhiteSpace(postcode) ? "" : "&postcode=" + Uri.EscapeDataString(postcode));
            var url = "https://api.geoapify.com/v1/geocode/search?format=json&limit=5" + query
                + "&filter=" + Uri.EscapeDataString("countrycode:" + config.CountryCode)
                + "&bias=proximity:23.7275,37.9838&lang=" + Uri.EscapeDataString(language)
                + (type is null ? "" : "&type=" + type)
                + "&apiKey=" + Uri.EscapeDataString(config.ApiKey);
            using var response = await _client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return document.RootElement.GetProperty("results").EnumerateArray().Take(5).Select(item => item.Clone()).ToArray();
        }
        /// <summary>Reads optional structured address fields without requiring provider coverage.</summary>
        private static string? ReadString(JsonElement item, string name)
            => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        /// <inheritdoc/>
        public void Dispose() { _client.Dispose(); _gate.Dispose(); }
    }
}
