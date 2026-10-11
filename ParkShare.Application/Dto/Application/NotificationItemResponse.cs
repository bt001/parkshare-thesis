namespace ParkShare.Application.Dto.Application
{
    /// <summary>NotificationItemResponse data contract.</summary>
    public class NotificationItemResponse
    {
        /// <summary>Notification identity.</summary>
        public long Id { get; set; }
        /// <summary>Optional related booking.</summary>
        public long? BookingId { get; set; }
        /// <summary>Stable event type.</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Server-generated event message.</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Whether the recipient has read this event.</summary>
        public bool IsRead { get; set; }
        /// <summary>UTC event timestamp.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
