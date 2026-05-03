using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="UserRole"/> table.
    /// </summary>
    public interface IUserRoleRepository
    {
        /// <summary>
        /// Checks whether a user-role link exists.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="roleId">Role ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> when the link exists.</returns>
        Task<bool> ExistsAsync(long userId, long roleId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a user-role link.
        /// </summary>
        /// <param name="userRole">User-role entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddAsync(UserRole userRole, CancellationToken cancellationToken);
    }
}
