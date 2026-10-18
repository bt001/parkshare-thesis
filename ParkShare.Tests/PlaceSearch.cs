using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParkShare.Application.Services;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Services;

internal static partial class Program
{
    /// <summary>Verifies the real provider adapter with deterministic HTTP responses, without spending credits.</summary>
    private static async Task PlaceSearch()
    {
        var options = new PlaceOptions();
        var transport = new PlaceTransport();
        using var provider = new GeoapifyPlaceSearchProvider(options, new HttpClient(transport));
        var service = new PlaceSearchService(provider, NullLogger<PlaceSearchService>.Instance);
        Check(!(await service.SearchAsync("Athens", Ct)).IsSuccessful && transport.Calls == 0, "missing key fails safely without HTTP");
        options.CurrentValue.ApiKey = "test-key";
        Check(!(await service.SearchAsync(" ", Ct)).IsSuccessful, "blank lookup rejected");
        var result = await service.SearchAsync("Syntagma", Ct);
        Check(result.IsSuccessful && result.Payload.Items.Single().Latitude == 37.9755, "provider result maps to geographic candidate");
        Check(transport.LastUri!.Contains("countrycode%3Agr") && transport.LastUri.Contains("lang=el") && transport.LastUri.Contains("bias=proximity:23.7275,37.9838"), "Greece and Athens request configuration");
        Check((await service.SearchAsync("syntagma", Ct)).IsSuccessful && transport.Calls == 1, "case-normalized lookup cache");
        transport.Status = HttpStatusCode.TooManyRequests;
        var failed = await service.SearchAsync("Acropolis", Ct);
        Check(!failed.IsSuccessful && !failed.ErrorsSummary.Contains("test-key"), "provider quota failure is safe and retryable");
        transport.Status = HttpStatusCode.OK;
        options.CurrentValue.DailyRequestLimit = 2;
        Check(!(await service.SearchAsync("Athens", Ct)).IsSuccessful && transport.Calls == 2, "local daily budget includes failed outbound requests");
        options.CurrentValue.DailyRequestLimit = 2500;
        transport.CityScenario = true;
        var city = await service.SearchAsync("Athens", Ct);
        Check(city.IsSuccessful && city.Payload.Items.First().Label == "Athens, Greece", "exact city promoted ahead of namesake businesses");
        transport.CityScenario = false;
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await service.SearchAsync("Monastiraki", cancelled.Token); Check(false, "caller cancellation must propagate"); }
        catch (OperationCanceledException) { }
    }

    /// <summary>Mutable options for deterministic provider tests.</summary>
    private sealed class PlaceOptions : IOptionsMonitor<PlaceSearchConfig>
    {
        /// <inheritdoc/>
        public PlaceSearchConfig CurrentValue { get; } = new();
        /// <inheritdoc/>
        public PlaceSearchConfig Get(string? name) => CurrentValue;
        /// <inheritdoc/>
        public IDisposable? OnChange(Action<PlaceSearchConfig, string?> listener) => null;
    }

    /// <summary>Captures outgoing lookup requests and supplies controlled provider responses.</summary>
    private sealed class PlaceTransport : HttpMessageHandler
    {
        /// <summary>Number of actual outgoing calls.</summary>
        public int Calls { get; private set; }
        /// <summary>Most recent request for parameter assertions.</summary>
        public string? LastUri { get; private set; }
        /// <summary>Response status used by the next call.</summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        /// <summary>Simulates a generic business hit followed by an exact city.</summary>
        public bool CityScenario { get; set; }
        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; LastUri = request.RequestUri!.OriginalString;
            if (CityScenario)
            {
                var city = LastUri.Contains("type=city");
                var body = city
                    ? """{"results":[{"formatted":"Athens, Greece","name":"Athens","result_type":"city","lat":37.9755,"lon":23.7348}]}"""
                    : """{"results":[{"formatted":"Athens Hotel","name":"Athens Hotel","result_type":"amenity","lat":37.98,"lon":23.73}]}""";
                return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body) });
            }
            return Task.FromResult(new HttpResponseMessage(Status)
            { Content = new StringContent("{\"results\":[{\"formatted\":\"Syntagma, Athens\",\"lat\":37.9755,\"lon\":23.7348}]}") });
        }
    }
}
