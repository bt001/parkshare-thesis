using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ParkShareDbContext _db;
        /// <summary>Creates message persistence using the request context.</summary>
        public ChatMessageRepository(ParkShareDbContext db) => _db = db;
        /// <inheritdoc/>
        public async Task<PagedResult<ChatMessage>> GetAsync(long bookingId, int page, int pageSize, CancellationToken ct)
        {
            var query = _db.ChatMessages.AsNoTracking().Where(m => m.BookingId == bookingId);
            var total = await query.CountAsync(ct);
            var offset = ((long)page - 1) * pageSize;
            if (offset >= total) return new PagedResult<ChatMessage>(new List<ChatMessage>(), total);
            var items = await query.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id)
                .Skip((int)offset).Take(pageSize).ToListAsync(ct);
            return new PagedResult<ChatMessage>(items, total);
        }
        /// <inheritdoc/>
        public async Task AddAsync(ChatMessage message, CancellationToken ct)
        {
            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync(ct);
        }
    }
}
