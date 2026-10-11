using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Chat
{
    /// <summary>GetBookingMessages API input.</summary>
    public class GetBookingMessagesRequest : IApiRequest
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated participant identity.</summary>
        public long UserId { get; set; }
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum messages per page.</summary>
        public int PageSize { get; set; } = 50;
    }
}
