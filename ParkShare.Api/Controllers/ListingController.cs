using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Listings;
using ParkShare.Shared.Results.Listings;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingController : BaseApiController
    {
        public ListingController(IMediator mediator, IMapper mapper)
            : base(mediator, mapper) { }

        [HttpPost]
        [ProducesResponseType(typeof(CreateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> Create([FromBody] CreateListingRequest request)
            => ProcessCommandAsync<CreateListingRequest, CreateListingResult, CreateListingCommand, CreateListingResponse>(request);

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(GetListingByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> GetById([FromRoute] long id)
            => ProcessQueryAsync<GetListingByIdRequest, GetListingByIdResult, GetListingByIdQuery, GetListingByIdResponse>(new GetListingByIdRequest { Id = id });

        [HttpGet("owner/{ownerId:long}")]
        [ProducesResponseType(typeof(GetOwnerListingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> GetByOwner([FromRoute] long ownerId)
            => ProcessQueryAsync<GetOwnerListingsRequest, GetOwnerListingsResult, GetOwnerListingsQuery, GetOwnerListingsResponse>(new GetOwnerListingsRequest { OwnerId = ownerId });

        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(UpdateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateListingRequest body)
            => ProcessCommandAsync<UpdateListingRequest, UpdateListingResult, UpdateListingCommand, UpdateListingResponse>(new UpdateListingRequest
            {
                Id = id,
                OwnerId = body.OwnerId,
                Title = body.Title,
                Description = body.Description,
                PricePerHour = body.PricePerHour,
                Latitude = body.Latitude,
                Longitude = body.Longitude,
                Address = body.Address,
                Size = body.Size,
                IsAlwaysAvailable = body.IsAlwaysAvailable
            });

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(DeactivateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Deactivate([FromBody] DeactivateListingRequest body)
          => ProcessCommandAsync<DeactivateListingRequest, DeactivateListingResult, DeactivateListingCommand, DeactivateListingResponse>(body);

        [HttpPatch("[action]")]
        [ProducesResponseType(typeof(ActivateListingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Activate([FromBody] ActivateListingRequest body)
            => ProcessCommandAsync<ActivateListingRequest, ActivateListingResult, ActivateListingCommand, ActivateListingResponse>(body);

        [HttpGet("search")]
        [ProducesResponseType(typeof(SearchListingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> Search([FromQuery] SearchListingsRequest query)
             => ProcessQueryAsync<SearchListingsRequest, SearchListingsResult, SearchListingsQuery, SearchListingsResponse>(query);

    }
}
