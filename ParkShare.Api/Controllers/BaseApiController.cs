using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto;
using ParkShare.Application.Interfaces;
using ParkShare.Shared.Results;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ParkShare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly IMediator _mediator;
        protected readonly IMapper _mapper;

        protected BaseApiController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Reads the authenticated user ID or returns an unauthorized result.
        /// </summary>
        /// <param name="currentUser">Current user context.</param>
        /// <param name="userId">Current user ID.</param>
        /// <returns>Unauthorized result when the user is missing; otherwise <c>null</c>.</returns>
        protected IActionResult? RequireCurrentUserId(ICurrentUserService currentUser, out long userId)
        {
            userId = currentUser.UserId.GetValueOrDefault();
            return currentUser.IsAuthenticated && userId > 0
                ? null
                : Unauthorized(new[] { "Authentication is required." });
        }

        protected async Task<IActionResult> ProcessQueryAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(
            TApiRequest apiRequest)
            where TApiRequest : IApiRequest
            where TApiResult : IApiResult
            where TAppRequest : IRequest<ServiceResult<TAppResponse>>
            where TAppResponse : class
        {
            var appRequest = _mapper.Map<TAppRequest>(apiRequest);
            var appResult = await _mediator.Send(appRequest, HttpContext.RequestAborted);
            return HandleQueryResult<TAppResponse, TApiResult>(appResult);
        }

        protected async Task<IActionResult> ProcessCommandAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(
            TApiRequest apiRequest)
            where TApiRequest : IApiRequest
            where TApiResult : IApiResult
            where TAppRequest : IRequest<ServiceResult<TAppResponse>>
            where TAppResponse : class
        {
            var appRequest = _mapper.Map<TAppRequest>(apiRequest);
            var appResult = await _mediator.Send(appRequest, HttpContext.RequestAborted);
            return HandleCommandResult<TAppResponse, TApiResult>(appResult);
        }

        /// <summary>
        /// Processes an authenticated query after applying the current user ID to the request.
        /// </summary>
        /// <param name="apiRequest">API request.</param>
        /// <param name="currentUser">Current user context.</param>
        /// <param name="setCurrentUserId">Applies the current user ID to the request.</param>
        /// <returns>Action result.</returns>
        protected Task<IActionResult> ProcessAuthenticatedQueryAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(
            TApiRequest apiRequest,
            ICurrentUserService currentUser,
            Action<TApiRequest, long> setCurrentUserId)
            where TApiRequest : IApiRequest
            where TApiResult : IApiResult
            where TAppRequest : IRequest<ServiceResult<TAppResponse>>
            where TAppResponse : class
        {
            var unauthorized = RequireCurrentUserId(currentUser, out var userId);
            if (unauthorized is not null)
                return Task.FromResult(unauthorized);

            setCurrentUserId(apiRequest, userId);
            return ProcessQueryAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(apiRequest);
        }

        /// <summary>
        /// Processes an authenticated command after applying the current user ID to the request.
        /// </summary>
        /// <param name="apiRequest">API request.</param>
        /// <param name="currentUser">Current user context.</param>
        /// <param name="setCurrentUserId">Applies the current user ID to the request.</param>
        /// <returns>Action result.</returns>
        protected Task<IActionResult> ProcessAuthenticatedCommandAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(
            TApiRequest apiRequest,
            ICurrentUserService currentUser,
            Action<TApiRequest, long> setCurrentUserId)
            where TApiRequest : IApiRequest
            where TApiResult : IApiResult
            where TAppRequest : IRequest<ServiceResult<TAppResponse>>
            where TAppResponse : class
        {
            var unauthorized = RequireCurrentUserId(currentUser, out var userId);
            if (unauthorized is not null)
                return Task.FromResult(unauthorized);

            setCurrentUserId(apiRequest, userId);
            return ProcessCommandAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(apiRequest);
        }

        protected IActionResult HandleQueryResult<TAppResponse, TApiResult>(ServiceResult<TAppResponse> result)
            where TAppResponse : class
            where TApiResult : IApiResult
        {
            if (result is null)
                return StatusCode(500, "");

            if (!result.IsSuccessful)
            {
                if (result.IsConflict)
                    return Conflict(result.Errors);
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                    return NotFound(result.Errors);

                if (result.Errors.Any(IsUnauthorizedError))
                    return Unauthorized(result.Errors);

                return BadRequest(result.Errors); // Fallback
            }

            var apiResult = _mapper.Map<TApiResult>(result.Payload);
            return Ok(apiResult);
        }

        protected IActionResult HandleCommandResult<TAppResponse, TApiResult>(ServiceResult<TAppResponse> result)
           where TAppResponse : class
           where TApiResult : IApiResult
        {
            if (result is null)
                return StatusCode(500, "Unexpected null result.");

            if (!result.IsSuccessful)
            {
                if (result.IsConflict)
                    return Conflict(result.Errors);
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                    return NotFound(result.Errors);

                if (result.Errors.Any(IsUnauthorizedError))
                    return Unauthorized(result.Errors);

                return BadRequest(result.Errors); // Fallback
            }

            var apiResult = _mapper.Map<TApiResult>(result.Payload);
            return Ok(apiResult);
        }

        /// <summary>
        /// Checks whether an error message represents an authorization failure.
        /// </summary>
        /// <param name="error">Error message.</param>
        /// <returns><c>true</c> when the error is authorization-related.</returns>
        private static bool IsUnauthorizedError(string error)
            => error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
                || error.Contains("not authorized", StringComparison.OrdinalIgnoreCase);
    }
}
