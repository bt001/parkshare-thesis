namespace ParkShare.Application.Dto.Infrastructure
{
    /// <summary>
    /// Represents issued authentication tokens.
    /// </summary>
    public class TokenPair
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
