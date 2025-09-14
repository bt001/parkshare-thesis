using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Bookings;
using ParkShare.Shared.Results.Bookings;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [ApiController]
    public class BookingController : BaseApiController
    {
        private readonly ILogger<BookingController> _logger;

        public BookingController(ILogger<BookingController> logger, IMediator mediator, IMapper mapper) : base(mediator, mapper)
        {
            _logger = logger;
        }

        [HttpPost("request")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        public Task<IActionResult> BookingRequest([FromBody] CreateBookingRequest body)
            => ProcessCommandAsync<CreateBookingRequest, BookingResult, CreateBookingCommand, CreateBookingResponse>(body);

        [HttpPatch("approve")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        public Task<IActionResult> ApproveBooking([FromBody] ApproveBookingRequest body)
            => ProcessCommandAsync<ApproveBookingRequest, BookingResult, ApproveBookingCommand, ApproveBookingResponse>(body);

        [HttpPatch("reject")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        public Task<IActionResult> RejectBooking([FromBody] RejectBookingRequest body)
            => ProcessCommandAsync<RejectBookingRequest, BookingResult, RejectBookingCommand, RejectBookingResponse>(body);

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(BookingResult), StatusCodes.Status200OK)]
        public Task<IActionResult> GetBookingById([FromRoute] long id)
            => ProcessQueryAsync<GetBookingByIdRequest, BookingResult, GetBookingByIdQuery, GetBookingByIdResponse>(new GetBookingByIdRequest { Id = id });

        [HttpGet("owner")]
        [ProducesResponseType(typeof(GetOwnerBookingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> GetOwnerBookings([FromQuery] GetOwnerBookingsRequest query)
            => ProcessQueryAsync<GetOwnerBookingsRequest, GetOwnerBookingsResult, GetOwnerBookingsQuery, GetOwnerBookingsResponse>(query);
    }
}
