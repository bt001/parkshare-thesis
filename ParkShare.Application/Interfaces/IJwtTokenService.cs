using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Creates and validates JWT tokens.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Creates a token pair for the provided user.
        /// </summary>
        /// <param name="user">Authenticated user.</param>
        /// <returns>Issued token pair.</returns>
        TokenPair CreateTokenPair(User user);

        /// <summary>
        /// Returns the user ID represented by a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <returns>User ID when valid; otherwise <c>null</c>.</returns>
        long? GetUserIdFromRefreshToken(string refreshToken);
    }
}
