using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Bookings;
using ParkShare.Shared.Results.Bookings;
using ParkShare.Shared.Requests.Chat;
using ParkShare.Shared.Results.Chat;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : BaseApiController
    {
        private readonly ILogger<BookingController> _logger;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="mapper">Mapper instance.</param>
        /// <param name="currentUser">Current user context.</param>
        public BookingController(ILogger<BookingController> logger, IMediator mediator, IMapper mapper, ICurrentUserService currentUser) : base(mediator, mapper)
        {
            _logger = logger;
            _currentUser = currentUser ?? throw new System.ArgumentNullException(nameof(currentUser));
        }

        [HttpPost("[action]")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> BookingRequest([FromBody] CreateBookingRequest body)
            => ProcessAuthenticatedCommandAsync<CreateBookingRequest, BookingResult, CreateBookingCommand, CreateBookingResponse>(
                body,
                _currentUser,
                (request, userId) => request.RenterId = userId);

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status409Conflict)]
        public Task<IActionResult> ApproveBooking([FromBody] ApproveBookingRequest body)
            => ProcessAuthenticatedCommandAsync<ApproveBookingRequest, BookingResult, ApproveBookingCommand, ApproveBookingResponse>(
                body,
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status409Conflict)]
        public Task<IActionResult> RejectBooking([FromBody] RejectBookingRequest body)
            => ProcessAuthenticatedCommandAsync<RejectBookingRequest, BookingResult, RejectBookingCommand, RejectBookingResponse>(
                body,
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        /// <summary>Reads the authenticated participant's booking conversation.</summary>
        [HttpGet("{id:long}/messages")]
        [ProducesResponseType(typeof(GetBookingMessagesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetMessages([FromRoute] long id, [FromQuery] GetBookingMessagesRequest query)
        {
            query.BookingId = id;
            return ProcessAuthenticatedQueryAsync<GetBookingMessagesRequest, GetBookingMessagesResult, GetBookingMessagesQuery, GetBookingMessagesResponse>(
                query, _currentUser, (request, userId) => request.UserId = userId);
        }

        /// <summary>Sends plain text as the authenticated booking participant.</summary>
        [HttpPost("{id:long}/messages")]
        [ProducesResponseType(typeof(ChatMessageResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> SendMessage([FromRoute] long id, [FromBody] SendBookingMessageRequest body)
        {
            body.BookingId = id;
            return ProcessAuthenticatedCommandAsync<SendBookingMessageRequest, ChatMessageResult, SendBookingMessageCommand, ChatMessageResponse>(
                body, _currentUser, (request, userId) => request.UserId = userId);
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetBookingById([FromRoute] long id)
            => ProcessAuthenticatedQueryAsync<GetBookingByIdRequest, BookingResult, GetBookingByIdQuery, GetBookingByIdResponse>(
                new GetBookingByIdRequest { Id = id },
                _currentUser,
                (request, userId) => request.RequesterId = userId);

        /// <summary>Returns booking history for the authenticated renter.</summary>
        [HttpGet("renter")]
        [ProducesResponseType(typeof(GetRenterBookingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetRenterBookings([FromQuery] GetRenterBookingsRequest query)
            => ProcessAuthenticatedQueryAsync<GetRenterBookingsRequest, GetRenterBookingsResult, GetRenterBookingsQuery, GetRenterBookingsResponse>(
                query, _currentUser, (request, userId) => request.RenterId = userId);

        [HttpGet("owner")]
        [ProducesResponseType(typeof(GetOwnerBookingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetOwnerBookings([FromQuery] GetOwnerBookingsRequest query)
            => ProcessAuthenticatedQueryAsync<GetOwnerBookingsRequest, GetOwnerBookingsResult, GetOwnerBookingsQuery, GetOwnerBookingsResponse>(
                query,
                _currentUser,
                (request, userId) => request.OwnerId = userId);
    }
}
