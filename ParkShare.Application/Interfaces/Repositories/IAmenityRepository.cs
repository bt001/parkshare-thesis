using ParkShare.Domain.Entities;
namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>Reads the amenity catalog.</summary>
    public interface IAmenityRepository
    {
        /// <summary>Returns catalog entries ordered by name and identifier.</summary>
        Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken ct);
    }
}
