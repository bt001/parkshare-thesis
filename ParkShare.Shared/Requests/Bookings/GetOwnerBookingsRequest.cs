using ParkShare.Common.Enums;
using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class GetOwnerBookingsRequest : IApiRequest
    {
        public long OwnerId { get; set; }
        public long? ListingId { get; set; }
        public List<BookingStatus>? Statuses { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
