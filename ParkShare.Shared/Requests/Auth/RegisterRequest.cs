using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Auth
{
    /// <summary>
    /// API registration request payload.
    /// </summary>
    public class RegisterRequest : IApiRequest
    {
        /// <summary>
        /// Requested unique username.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
