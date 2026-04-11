namespace ParkShare.Infrastructure.Configuration
{
    /// <summary>
    /// JWT bearer token configuration.
    /// </summary>
    public class JwtConfig
    {
        /// <summary>
        /// Token issuer.
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token audience.
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Symmetric signing key.
        /// </summary>
        public string SigningKey { get; set; } = string.Empty;

        /// <summary>
        /// Token lifetime in minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 120;

        /// <summary>
        /// Refresh token lifetime in days.
        /// </summary>
        public int RefreshExpirationDays { get; set; } = 7;
    }
}
