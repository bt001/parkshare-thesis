using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Auth;
using ParkShare.Shared.Results.Auth;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        public AuthController(IMediator mediator, IMapper mapper) : base(mediator, mapper) { }

        /// <summary>
        /// Authenticates a user by email and password.
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>User information on success</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
            =>  await ProcessQueryAsync<LoginRequest, LoginResult, LoginQuery, LoginResponse>(request);

    }
}
