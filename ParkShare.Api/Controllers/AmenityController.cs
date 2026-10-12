using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Amenities;
using ParkShare.Shared.Results.Amenities;
namespace ParkShare.Api.Controllers
{
    /// <summary>Exposes the public amenity catalog.</summary>
    [AllowAnonymous]
    public class AmenityController : BaseApiController
    {
        /// <summary>Creates the catalog controller.</summary>
        public AmenityController(IMediator mediator, IMapper mapper) : base(mediator, mapper) { }
        /// <summary>Returns available amenities ordered by name.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(GetAmenitiesResult), 200)]
        public Task<IActionResult> GetAll()
            => ProcessQueryAsync<GetAmenitiesRequest, GetAmenitiesResult, GetAmenitiesQuery, GetAmenitiesResponse>(new());
    }
}
