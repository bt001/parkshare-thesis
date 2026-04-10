namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Chat message for a booking.
    /// Linked to booking and sender.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Booking request ID.
        /// </summary>
        public long BookingId { get; set; }

        /// <summary>
        /// Parent booking request.
        /// </summary>
        public Booking Booking { get; set; } = default!;

        /// <summary>
        /// Sender user ID.
        /// </summary>
        public long SenderId { get; set; }

        /// <summary>
        /// Sender user.
        /// </summary>
        public User Sender { get; set; } = default!;

        /// <summary>
        /// ChatMessage text.
        /// </summary>
        public string MessageText { get; set; } = string.Empty;

        /// <summary>
        /// ChatMessage timestamp.
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
