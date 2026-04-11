using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Auth
{
    /// <summary>
    /// API request for refreshing authentication tokens.
    /// </summary>
    public class RefreshTokenRequest : IApiRequest
    {
        /// <summary>
        /// JWT refresh token.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
