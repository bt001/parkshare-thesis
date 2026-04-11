using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Listings;
using ParkShare.Shared.Results.Listings;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ListingController : BaseApiController
    {
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="mapper">Mapper instance.</param>
        /// <param name="currentUser">Current user context.</param>
        public ListingController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser)
            : base(mediator, mapper)
            => _currentUser = currentUser ?? throw new System.ArgumentNullException(nameof(currentUser));

        [HttpPost]
        [ProducesResponseType(typeof(CreateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Create([FromBody] CreateListingRequest body)
            => ProcessAuthenticatedCommandAsync<CreateListingRequest, CreateListingResult, CreateListingCommand, CreateListingResponse>(
                body,
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetListingByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> GetById([FromRoute] long id)
            => ProcessQueryAsync<GetListingByIdRequest, GetListingByIdResult, GetListingByIdQuery, GetListingByIdResponse>(new GetListingByIdRequest { Id = id });

        [HttpGet("owner")]
        [ProducesResponseType(typeof(GetOwnerListingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetByOwner()
            => ProcessAuthenticatedQueryAsync<GetOwnerListingsRequest, GetOwnerListingsResult, GetOwnerListingsQuery, GetOwnerListingsResponse>(
                new GetOwnerListingsRequest(),
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(UpdateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateListingRequest body)
            => ProcessAuthenticatedCommandAsync<UpdateListingRequest, UpdateListingResult, UpdateListingCommand, UpdateListingResponse>(
                new UpdateListingRequest
            {
                Id = id,
                Title = body.Title,
                Description = body.Description,
                PricePerHour = body.PricePerHour,
                Latitude = body.Latitude,
                Longitude = body.Longitude,
                Address = body.Address,
                Size = body.Size,
                IsAlwaysAvailable = body.IsAlwaysAvailable
            },
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(DeactivateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Deactivate([FromBody] DeactivateListingRequest body)
            => ProcessAuthenticatedCommandAsync<DeactivateListingRequest, DeactivateListingResult, DeactivateListingCommand, DeactivateListingResponse>(
                body,
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(ActivateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Activate([FromBody] ActivateListingRequest body)
            => ProcessAuthenticatedCommandAsync<ActivateListingRequest, ActivateListingResult, ActivateListingCommand, ActivateListingResponse>(
                body,
                _currentUser,
                (request, userId) => request.OwnerId = userId);

        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SearchListingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> Search([FromQuery] SearchListingsRequest query)
             => ProcessQueryAsync<SearchListingsRequest, SearchListingsResult, SearchListingsQuery, SearchListingsResponse>(query);

    }
}
