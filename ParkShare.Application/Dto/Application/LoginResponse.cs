namespace ParkShare.Application.Dto.Application
{
    public class LoginResponse
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
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
