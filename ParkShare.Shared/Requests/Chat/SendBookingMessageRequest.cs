using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Chat
{
    /// <summary>SendBookingMessage API input.</summary>
    public class SendBookingMessageRequest : IApiRequest
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated participant identity.</summary>
        public long UserId { get; set; }
        /// <summary>Plain-text message.</summary>
        public string MessageText { get; set; } = string.Empty;
    }
}
