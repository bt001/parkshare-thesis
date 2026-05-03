using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Authenticates users.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Registers a new user and returns token-backed identity data.
        /// </summary>
        /// <param name="userName">Requested username.</param>
        /// <param name="firstName">User first name.</param>
        /// <param name="lastName">User last name.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authenticated user data.</returns>
        Task<ServiceResult<LoginResponse>> RegisterUser(
            string userName,
            string firstName,
            string lastName,
            string email,
            string password,
            CancellationToken cancellationToken);

        /// <summary>
        /// Authenticates a user and returns token-backed identity data.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authenticated user data.</returns>
        Task<ServiceResult<LoginResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes authentication tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authenticated user data with refreshed tokens.</returns>
        Task<ServiceResult<LoginResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken);
    }
}
