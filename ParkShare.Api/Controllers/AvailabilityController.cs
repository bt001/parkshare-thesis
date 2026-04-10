using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
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
    public class AvailabilityController : BaseApiController
    {
        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="mapper">Mapper instance.</param>
        public AvailabilityController(IMediator mediator, IMapper mapper)
            : base(mediator, mapper)
        {
        }

        /// <summary>
        /// Creates an availability window.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AvailabilityWindowMutationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Create([FromBody] CreateAvailabilityWindowRequest request)
            => ProcessCommandAsync<CreateAvailabilityWindowRequest, AvailabilityWindowMutationResult, CreateAvailabilityWindowCommand, CreateAvailabilityWindowResponse>(request);

        /// <summary>
        /// Updates an availability window.
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(AvailabilityWindowMutationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateAvailabilityWindowRequest body)
            => ProcessCommandAsync<UpdateAvailabilityWindowRequest, AvailabilityWindowMutationResult, UpdateAvailabilityWindowCommand, UpdateAvailabilityWindowResponse>(new UpdateAvailabilityWindowRequest
            {
                Id = id,
                OwnerId = body.OwnerId,
                StartDateTime = body.StartDateTime,
                EndDateTime = body.EndDateTime,
                IsBlocked = body.IsBlocked,
                Note = body.Note
            });

        /// <summary>
        /// Deletes an availability window.
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(typeof(DeleteAvailabilityWindowResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Delete([FromRoute] long id, [FromQuery] long ownerId)
            => ProcessCommandAsync<DeleteAvailabilityWindowRequest, DeleteAvailabilityWindowResult, DeleteAvailabilityWindowCommand, DeleteAvailabilityWindowResponse>(new DeleteAvailabilityWindowRequest
            {
                Id = id,
                OwnerId = ownerId
            });

        /// <summary>
        /// Returns availability windows for a listing.
        /// </summary>
        [HttpGet("listing/{listingId:long}")]
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
        public Task<IActionResult> GetOwnerAvailability([FromQuery] GetOwnerAvailabilityRequest request)
            => ProcessQueryAsync<GetOwnerAvailabilityRequest, GetAvailabilityWindowsResult, GetOwnerAvailabilityQuery, GetAvailabilityWindowsResponse>(request);
    }
}
