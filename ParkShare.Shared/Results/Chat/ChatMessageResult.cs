namespace ParkShare.Shared.Results.Chat
{
    /// <summary>A persisted booking message.</summary>
    public class ChatMessageResult : IApiResult
    {
        /// <summary>Message identity.</summary>
        public long Id { get; set; }
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated sender identity.</summary>
        public long SenderId { get; set; }
        /// <summary>Plain-text message.</summary>
        public string MessageText { get; set; } = string.Empty;
        /// <summary>UTC send timestamp.</summary>
        public DateTime SentAt { get; set; }
    }
}
