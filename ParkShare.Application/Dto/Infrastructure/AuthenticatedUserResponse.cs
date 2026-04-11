namespace ParkShare.Application.Dto.Infrastructure
{
    /// <summary>
    /// Authenticated user data returned after successful login.
    /// </summary>
    public class AuthenticatedUserResponse : UserResponse
    {
        /// <summary>
        /// JWT bearer access token.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Access token expiration timestamp.
        /// </summary>
        public DateTime AccessTokenExpiresAt { get; set; }

        /// <summary>
        /// Access token type.
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// JWT refresh token.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token expiration timestamp.
        /// </summary>
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
