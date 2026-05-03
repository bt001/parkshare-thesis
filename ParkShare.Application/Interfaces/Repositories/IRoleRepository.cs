using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="Role"/> table.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Returns a role by ID.
        /// </summary>
        /// <param name="roleId">Role ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching role when found.</returns>
        Task<Role?> GetByIdAsync(long roleId, CancellationToken cancellationToken);
    }
}
