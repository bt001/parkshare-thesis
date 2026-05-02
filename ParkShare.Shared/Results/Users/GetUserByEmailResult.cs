using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Users
{
    /// <summary>
    /// API result for fetching a user by email.
    /// </summary>
    public class GetUserByEmailResult : IApiResult
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>Unique username for login/display</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Email address (unique).
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// Canonical serialized user roles.
        /// </summary>
        public string Role { get; set; } = default!;

        /// <summary>
        /// Individual user capability names.
        /// </summary>
        public string[] Roles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = default!;
    }
}
