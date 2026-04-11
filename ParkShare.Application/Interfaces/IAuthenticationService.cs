using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;

namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Authenticates users.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user and returns token-backed identity data.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authenticated user data.</returns>
        Task<ServiceResult<AuthenticatedUserResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes authentication tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authenticated user data with refreshed tokens.</returns>
        Task<ServiceResult<AuthenticatedUserResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken);
    }
}
