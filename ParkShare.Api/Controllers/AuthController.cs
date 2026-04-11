using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Auth;
using ParkShare.Shared.Results.Auth;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    /// <summary>
    /// Exposes authentication endpoints.
    /// </summary>
    /// <remarks>
    /// Authentication is intentionally kept lightweight for the thesis MVP so implementation effort stays focused on parking rental workflows rather than production-grade security features.
    /// </remarks>
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : BaseApiController
    {
        /// <summary>
        /// Creates the controller.
        /// </summary>
        /// <param name="mediator">Mediator instance.</param>
        /// <param name="mapper">Mapper instance.</param>
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

        /// <summary>
        /// Refreshes authentication tokens.
        /// </summary>
        /// <param name="request">Refresh token request.</param>
        /// <returns>Refreshed authentication data on success.</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
            => await ProcessQueryAsync<RefreshTokenRequest, LoginResult, RefreshTokenQuery, LoginResponse>(request);

    }
}
