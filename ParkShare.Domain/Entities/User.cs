using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// User account.
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

        /// <summary>Whether the stored credential uses the supported password hash format.</summary>
        public bool IsPasswordHashed { get; set; }

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Is user active (not deleted/deactivated)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Listings owned by the user.
        /// </summary>
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();

        /// <summary>
        /// Bookings created by the user as renter.
        /// </summary>
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        /// <summary>
        /// Chat messages sent by the user.
        /// </summary>
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// Notifications for the user.
        /// </summary>
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        /// <summary>
        /// Ratings created by the user as renter.
        /// </summary>
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        /// <summary>
        /// Assigned user roles.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
