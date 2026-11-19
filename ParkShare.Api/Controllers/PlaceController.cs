using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Places;
using ParkShare.Shared.Results.Places;
namespace ParkShare.Api.Controllers
{
    /// <summary>Resolves places for signed-in parking discovery.</summary>
    [Authorize]
    public class PlaceController : BaseApiController
    {
        /// <summary>Creates the place controller.</summary>
        public PlaceController(IMediator mediator, IMapper mapper) : base(mediator, mapper) { }
        /// <summary>Returns streets with locality information for owner address selection.</summary>
        [HttpGet("streets")]
        public Task<IActionResult> Streets([FromQuery] SearchPlacesRequest request)
            => ProcessQueryAsync<SearchPlacesRequest, SearchPlacesResult, SearchStreetsQuery, SearchPlacesResponse>(request);
        /// <summary>Resolves a structured building address.</summary>
        [HttpGet("address")]
        public Task<IActionResult> Address([FromQuery] ResolveAddressRequest request)
            => ProcessQueryAsync<ResolveAddressRequest, SearchPlacesResult, ResolveAddressQuery, SearchPlacesResponse>(request);
        /// <summary>Returns candidates for an explicitly submitted place name.</summary>
        [HttpGet("search")]
        public Task<IActionResult> Search([FromQuery] SearchPlacesRequest request)
            => ProcessQueryAsync<SearchPlacesRequest, SearchPlacesResult, SearchPlacesQuery, SearchPlacesResponse>(request);
    }
}
