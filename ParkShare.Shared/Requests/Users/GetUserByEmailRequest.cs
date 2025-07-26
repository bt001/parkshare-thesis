using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Users
{
    /// <summary>
    /// Represents a request to search for a user by email.
    /// </summary>
    /// <remarks>This request is typically used in APIs to query user information based on their email
    /// address.</remarks>
    public class GetUserByEmailRequest : IApiRequest
    {
        /// <summary>
        /// Email address associated with the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
