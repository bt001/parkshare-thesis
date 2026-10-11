using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>Persists booking conversation messages.</summary>
    public interface IChatMessageRepository
    {
        /// <summary>Returns messages newest first with identity breaking timestamp ties.</summary>
        Task<PagedResult<ChatMessage>> GetAsync(long bookingId, int page, int pageSize, CancellationToken ct);
        /// <summary>Saves one message and assigns its identity.</summary>
        Task AddAsync(ChatMessage message, CancellationToken ct);
    }
}
