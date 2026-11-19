using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Swappable external location lookup, independent of stored parking addresses.</summary>
    public interface IPlaceSearchProvider
    {
        /// <summary>Resolves a submitted name; throws on unavailable or invalid provider responses.</summary>
        Task<IReadOnlyList<PlaceResponse>> SearchAsync(string text, CancellationToken ct);
        /// <summary>Finds streets with structured locality information.</summary>
        Task<IReadOnlyList<PlaceResponse>> SearchStreetsAsync(string text, CancellationToken ct);
        /// <summary>Resolves a structured building address.</summary>
        Task<IReadOnlyList<PlaceResponse>> ResolveAddressAsync(string street, string houseNumber, string? city, string? postcode, CancellationToken ct);
    }
}
