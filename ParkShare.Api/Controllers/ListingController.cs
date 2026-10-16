using ParkShare.Shared.Requests.Ratings;
using ParkShare.Shared.Results.Ratings;
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

        /// <summary>Returns public reviews and the listing aggregate.</summary>
        [HttpGet("{id:long}/ratings")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GetListingRatingsResult), StatusCodes.Status200OK)]
        public Task<IActionResult> GetRatings([FromRoute] long id, [FromQuery] GetListingRatingsRequest query)
        {
            query.ListingId = id;
            return ProcessQueryAsync<GetListingRatingsRequest, GetListingRatingsResult, GetListingRatingsQuery, GetListingRatingsResponse>(query);
        }

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
                AmenityIds = body.AmenityIds,
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

        /// <summary>Uploads one bounded JPEG or PNG for the authenticated listing owner.</summary>
        [HttpPost("{id:long}/photos")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ParkShare.Common.Constants.ListingPhotoLimits.MaxBytes + 65536)]
        [RequestFormLimits(MultipartBodyLengthLimit = ParkShare.Common.Constants.ListingPhotoLimits.MaxBytes + 65536)]
        [ProducesResponseType(typeof(ListingPhotoResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadPhoto([FromRoute] long id, IFormFile file)
        {
            var unauthorized = RequireCurrentUserId(_currentUser, out var userId);
            if (unauthorized is not null) return unauthorized;
            if (file is null || file.Length <= 0 || file.Length > ParkShare.Common.Constants.ListingPhotoLimits.MaxBytes
                || Request.Form.Files.Count != 1)
                return BadRequest(new[] { "Select one JPEG or PNG image up to 5 MiB." });
            await using var stream = file.OpenReadStream();
            var result = await _mediator.Send(new UploadListingPhotoCommand
            {
                ListingId = id, OwnerId = userId, Content = stream, ContentType = file.ContentType
            }, HttpContext.RequestAborted);
            return HandleCommandResult<ListingPhotoResponse, ListingPhotoResult>(result);
        }

        /// <summary>Removes a photo belonging to the authenticated owner's listing.</summary>
        [HttpDelete("{id:long}/photos/{photoId:long}")]
        [ProducesResponseType(typeof(DeleteListingPhotoResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeletePhoto([FromRoute] long id, [FromRoute] long photoId)
        {
            var unauthorized = RequireCurrentUserId(_currentUser, out var userId);
            if (unauthorized is not null) return unauthorized;
            var result = await _mediator.Send(new DeleteListingPhotoCommand
            {
                ListingId = id, PhotoId = photoId, OwnerId = userId
            }, HttpContext.RequestAborted);
            return HandleCommandResult<DeleteListingPhotoResponse, DeleteListingPhotoResult>(result);
        }

        /// <summary>Returns public photos in stable upload order.</summary>
        [HttpGet("{id:long}/photos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ListingPhotosResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPhotos([FromRoute] long id)
        {
            var result = await _mediator.Send(new GetListingPhotosQuery { ListingId = id }, HttpContext.RequestAborted);
            return HandleQueryResult<ListingPhotosResponse, ListingPhotosResult>(result);
        }

        /// <summary>Serves a stored photo only while its matching metadata exists.</summary>
        [HttpGet("{id:long}/photos/{photoId:long}/content")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPhotoContent([FromRoute] long id, [FromRoute] long photoId)
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.XContentTypeOptions = "nosniff";
            var result = await _mediator.Send(new GetListingPhotoContentQuery { ListingId = id, PhotoId = photoId }, HttpContext.RequestAborted);
            if (!result.IsSuccessful)
            {
                if (System.Linq.Enumerable.Any(result.Errors, e => e.Contains("not found", System.StringComparison.OrdinalIgnoreCase)))
                    return NotFound(result.Errors);
                return BadRequest(result.Errors);
            }
            return File(result.Payload.Content, result.Payload.ContentType);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SearchListingsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> Search([FromQuery] SearchListingsRequest query)
             => ProcessQueryAsync<SearchListingsRequest, SearchListingsResult, SearchListingsQuery, SearchListingsResponse>(query);

    }
}
