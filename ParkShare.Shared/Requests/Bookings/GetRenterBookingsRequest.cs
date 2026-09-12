using ParkShare.Common.Enums;
using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    /// <summary>Requests the authenticated renter booking history.</summary>
    public class GetRenterBookingsRequest : IApiRequest
    {
        /// <summary>Renter ID, overwritten by the API from the authenticated user.</summary>
        public long RenterId { get; set; }
        /// <summary>Optional booking statuses.</summary>
        public List<BookingStatus>? Statuses { get; set; }
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum number of bookings per page.</summary>
        public int PageSize { get; set; } = 20;
    }
}
