using ParkShare.Domain.Entities;

namespace ParkShare.Application.Dto.Infrastructure
{
    /// <summary>Approval changes awaiting commit inside the caller's transaction.</summary>
    public class BookingApprovalResult
    {
        /// <summary>Approved booking with its listing.</summary>
        public Booking Booking { get; init; } = default!;
        /// <summary>Previously pending bookings rejected by this approval.</summary>
        public List<Booking> RejectedBookings { get; init; } = new();
    }
}
