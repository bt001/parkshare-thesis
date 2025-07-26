using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Dto;
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

        protected async Task<IActionResult> ProcessQueryAsync<TApiRequest, TApiResult, TAppRequest, TAppResponse>(
            TApiRequest apiRequest)
            where TApiRequest : IApiRequest
            where TApiResult : IApiResult
            where TAppRequest : IRequest<ServiceResult<TAppResponse>>
            where TAppResponse : class
        {
            var appRequest = _mapper.Map<TAppRequest>(apiRequest);
            var appResult = await _mediator.Send(appRequest);
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
            var appResult = await _mediator.Send(appRequest);
            return HandleCommandResult<TAppResponse, TApiResult>(appResult);
        }

        private IActionResult HandleQueryResult<TAppResponse, TApiResult>(ServiceResult<TAppResponse> result)
            where TAppResponse : class
            where TApiResult : IApiResult
        {
            if (result is null)
                return StatusCode(500, "");

            if (!result.IsSuccessful)
            {
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                    return NotFound(result.Errors);

                if (result.Errors.Any(e => e.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
                    return Unauthorized(result.Errors);

                return BadRequest(result.Errors); // Fallback
            }

            var apiResult = _mapper.Map<TApiResult>(result.Payload);
            return Ok(apiResult);
        }

        private IActionResult HandleCommandResult<TAppResponse, TApiResult>(ServiceResult<TAppResponse> result)
           where TAppResponse : class
           where TApiResult : IApiResult
        {
            if (result is null)
                return StatusCode(500, "Unexpected null result.");

            if (!result.IsSuccessful)
            {
                if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                    return NotFound(result.Errors);

                if (result.Errors.Any(e => e.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
                    return Unauthorized(result.Errors);

                return BadRequest(result.Errors); // Fallback
            }

            var apiResult = _mapper.Map<TApiResult>(result.Payload);
            return Ok(apiResult);
        }
    }
}
