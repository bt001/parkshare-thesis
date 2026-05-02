namespace ParkShare.Shared.Results.Auth
{
    /// <summary>
    /// API login result payload.
    /// </summary>
    public class LoginResult : IApiResult
    {
        /// <summary>
        /// Authenticated user identifier.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Authenticated user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Authenticated user email.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Canonical serialized user capabilities.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Individual user capability names.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Authenticated display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
