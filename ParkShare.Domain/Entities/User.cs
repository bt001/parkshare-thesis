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

        /// <summary>
        /// Email address (unique).
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Password hash.
        /// </summary>
        public string PasswordHash { get; set; } = null!;

        /// <summary>
        /// Display name.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// User role: Owner, Renter, or both.
        /// </summary>
        public string Role { get; set; } = null!;

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Booking> BookingRequests { get; set; } = new List<Booking>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
