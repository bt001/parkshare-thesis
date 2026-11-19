using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    /// <summary>Provides participant-only booking conversations.</summary>
    public interface IChatService
    {
        /// <summary>Acknowledges at most 100 explicitly displayed incoming message identities.</summary>
        Task<ServiceResult<ReadMessagesResponse>> MarkReadAsync(long bookingId, long userId, IReadOnlyCollection<long>? ids, CancellationToken ct);
        /// <summary>Reads a bounded page after checking booking participation.</summary>
        Task<ServiceResult<GetBookingMessagesResponse>> GetAsync(long bookingId, long userId, int page, int pageSize, CancellationToken ct);
        /// <summary>Persists a bounded text message from an authenticated participant.</summary>
        Task<ServiceResult<ChatMessageResponse>> SendAsync(long bookingId, long userId, string text, CancellationToken ct);
    }
}
