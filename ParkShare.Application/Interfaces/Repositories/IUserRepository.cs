using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="User"/> table.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Checks whether a user exists by ID.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> when the user exists.</returns>
        Task<bool> ExistsByIdAsync(long userId, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a user exists by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> when a matching user exists.</returns>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a user exists by username.
        /// </summary>
        /// <param name="userName">Username.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> when a matching user exists.</returns>
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a user row.
        /// </summary>
        /// <param name="user">User entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddAsync(User user, CancellationToken cancellationToken);

        /// <summary>Persists a replacement hash for an already converted account.</summary>
        Task UpdatePasswordHashAsync(long userId, string passwordHash, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a user by email with assigned roles loaded.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching user when found.</returns>
        Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a user by ID with assigned roles loaded.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching user when found.</returns>
        Task<User?> GetByIdWithRolesAsync(long userId, CancellationToken cancellationToken);
    }
}
