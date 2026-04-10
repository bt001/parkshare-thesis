namespace ParkShare.Application.Dto.Infrastructure
{
    public class UserResponse
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
        /// User role: Owner, Renter, or both.
        /// </summary>
        public string Role { get; set; } = default!;

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = default!;

        /// <summary>
        /// Is user active (not deleted/deactivated)
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
