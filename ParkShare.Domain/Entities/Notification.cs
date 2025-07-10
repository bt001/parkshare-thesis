namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// In-app notification.
    /// Sent to user for booking events.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// User ID.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Recipient user.
        /// </summary>
        public User User { get; set; } = default!;

        /// <summary>
        /// Notification type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Notification message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Read status.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Notification timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
