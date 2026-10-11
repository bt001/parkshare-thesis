using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class ChatService : IChatService
    {
        private readonly IBookingRepository _bookings;
        private readonly IChatMessageRepository _messages;
        private readonly TimeProvider _clock;
        private readonly ILogger<ChatService> _logger;
        /// <summary>Creates the participant conversation service.</summary>
        public ChatService(IBookingRepository bookings, IChatMessageRepository messages, TimeProvider clock, ILogger<ChatService> logger)
        { _bookings = bookings; _messages = messages; _clock = clock; _logger = logger; }

        /// <inheritdoc/>
        public Task<ServiceResult<GetBookingMessagesResponse>> GetAsync(long bookingId, long userId, int page, int pageSize, CancellationToken ct)
            => ExecuteAsync(async () =>
            {
                var booking = await FindParticipantBookingAsync(bookingId, userId, ct);
                if (booking is null) return ServiceResult<GetBookingMessagesResponse>.WithError("Booking not found.");
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return ServiceResult<GetBookingMessagesResponse>.WithError("Invalid message page.");
                var result = await _messages.GetAsync(bookingId, page, pageSize, ct);
                return ServiceResult<GetBookingMessagesResponse>.Success(new()
                {
                    Page = page, PageSize = pageSize, TotalCount = result.TotalCount,
                    OwnerId = booking.Listing.OwnerId, RenterId = booking.RenterId,
                    Items = result.Items.Select(ToResponse).ToList()
                });
            }, bookingId, ct);

        /// <inheritdoc/>
        public Task<ServiceResult<ChatMessageResponse>> SendAsync(long bookingId, long userId, string text, CancellationToken ct)
            => ExecuteAsync(async () =>
            {
                var booking = await FindParticipantBookingAsync(bookingId, userId, ct);
                if (booking is null) return ServiceResult<ChatMessageResponse>.WithError("Booking not found.");
                var trimmed = text?.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 2000)
                    return ServiceResult<ChatMessageResponse>.WithError("Message must contain 1 to 2000 characters.");
                var message = new ChatMessage { BookingId = bookingId, SenderId = userId, MessageText = trimmed, SentAt = _clock.GetUtcNow().UtcDateTime };
                await _messages.AddAsync(message, ct);
                return ServiceResult<ChatMessageResponse>.Success(ToResponse(message));
            }, bookingId, ct);

        /// <summary>Checks participation without exposing inaccessible booking existence.</summary>
        private async Task<Booking?> FindParticipantBookingAsync(long bookingId, long userId, CancellationToken ct)
        {
            var booking = await _bookings.GetByIdWithListingAsync(bookingId, ct);
            return userId > 0 && booking is not null && (booking.RenterId == userId || booking.Listing.OwnerId == userId) ? booking : null;
        }
        /// <summary>Restores UTC metadata after SQL datetime materialization.</summary>
        private static ChatMessageResponse ToResponse(ChatMessage message) => new()
        {
            Id = message.Id, BookingId = message.BookingId, SenderId = message.SenderId,
            MessageText = message.MessageText, SentAt = DateTime.SpecifyKind(message.SentAt, DateTimeKind.Utc)
        };
        /// <summary>Logs operational failures without logging text or swallowing caller cancellation.</summary>
        private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, long bookingId, CancellationToken ct) where T : notnull
        {
            try { return await operation(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat operation failed for booking {BookingId}.", bookingId);
                return ServiceResult<T>.WithError("Messages could not be loaded or sent. Please try again.");
            }
        }
    }
}
