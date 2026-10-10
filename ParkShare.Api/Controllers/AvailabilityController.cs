using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Availability;
using ParkShare.Shared.Results.Availability;
using System;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    /// <summary>
    /// Exposes availability management endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AvailabilityController : BaseApiController
    {
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="mapper">Mapper instance.</param>
        /// <param name="currentUser">Current user context.</param>
        public AvailabilityController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser)
            : base(mediator, mapper)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <summary>
        /// Creates an availability window.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AvailabilityWindowMutationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status409Conflict)]
        public Task<IActionResult> Create([FromBody] CreateAvailabilityWindowRequest request)
            => ProcessAuthenticatedCommandAsync<CreateAvailabilityWindowRequest, AvailabilityWindowMutationResult, CreateAvailabilityWindowCommand, CreateAvailabilityWindowResponse>(
                request,
                _currentUser,
                (apiRequest, userId) => apiRequest.OwnerId = userId);

        /// <summary>
        /// Updates an availability window.
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(AvailabilityWindowMutationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status409Conflict)]
        public Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateAvailabilityWindowRequest body)
            => ProcessAuthenticatedCommandAsync<UpdateAvailabilityWindowRequest, AvailabilityWindowMutationResult, UpdateAvailabilityWindowCommand, UpdateAvailabilityWindowResponse>(
                new UpdateAvailabilityWindowRequest
            {
                Id = id,
                StartDateTime = body.StartDateTime,
                EndDateTime = body.EndDateTime,
                IsBlocked = body.IsBlocked,
                Note = body.Note
            },
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        /// <summary>
        /// Deletes an availability window.
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(typeof(DeleteAvailabilityWindowResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Delete([FromRoute] long id)
            => ProcessAuthenticatedCommandAsync<DeleteAvailabilityWindowRequest, DeleteAvailabilityWindowResult, DeleteAvailabilityWindowCommand, DeleteAvailabilityWindowResponse>(
                new DeleteAvailabilityWindowRequest { Id = id },
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        /// <summary>
        /// Returns availability windows for a listing.
        /// </summary>
        [HttpGet("listing/{listingId:long}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetAvailabilityWindowsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetListingAvailability([FromRoute] long listingId, [FromQuery] DateTime? startDateTime, [FromQuery] DateTime? endDateTime)
            => ProcessQueryAsync<GetListingAvailabilityRequest, GetAvailabilityWindowsResult, GetListingAvailabilityQuery, GetAvailabilityWindowsResponse>(new GetListingAvailabilityRequest
            {
                ListingId = listingId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime
            });

        /// <summary>
        /// Returns availability windows for an owner.
        /// </summary>
        [HttpGet("owner")]
        [ProducesResponseType(typeof(GetAvailabilityWindowsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetOwnerAvailability([FromQuery] GetOwnerAvailabilityRequest request)
            => ProcessAuthenticatedQueryAsync<GetOwnerAvailabilityRequest, GetAvailabilityWindowsResult, GetOwnerAvailabilityQuery, GetAvailabilityWindowsResponse>(
                request,
                _currentUser,
                (apiRequest, userId) => apiRequest.OwnerId = userId);
    }
}
