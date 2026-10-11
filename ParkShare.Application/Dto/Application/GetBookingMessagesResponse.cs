namespace ParkShare.Application.Dto.Application
{
    /// <summary>A participant conversation page.</summary>
    public class GetBookingMessagesResponse
    {
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum messages per page.</summary>
        public int PageSize { get; set; } = 50;
        /// <summary>Total conversation messages.</summary>
        public int TotalCount { get; set; }
        /// <summary>Booking owner identity.</summary>
        public long OwnerId { get; set; }
        /// <summary>Booking renter identity.</summary>
        public long RenterId { get; set; }
        /// <summary>Newest messages first.</summary>
        public List<ChatMessageResponse> Items { get; set; } = new();
    }
}
