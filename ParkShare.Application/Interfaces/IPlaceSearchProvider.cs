using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Swappable external location lookup, independent of stored parking addresses.</summary>
    public interface IPlaceSearchProvider
    {
        /// <summary>Resolves a submitted name; throws on unavailable or invalid provider responses.</summary>
        Task<IReadOnlyList<PlaceResponse>> SearchAsync(string text, CancellationToken ct);
    }
}
