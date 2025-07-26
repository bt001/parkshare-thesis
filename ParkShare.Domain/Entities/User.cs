namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// User account.
    /// Can be owner, renter, or both.
    /// </summary>
    public class User
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
        public string Email { get; set; } = null!;

        /// <summary>
        /// Password hash.
        /// </summary>
        public string PasswordHash { get; set; } = null!;

        /// <summary>
        /// User role: Owner, Renter, or both.
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Is user active (not deleted/deactivated)
        /// </summary>
        public bool IsActive { get; set; } = true;

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
