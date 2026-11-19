using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Shared.Requests.Activity;
using ParkShare.Shared.Results.Activity;
namespace ParkShare.Api.Controllers;
/// <summary>Authenticated navigation activity.</summary>
[Authorize]
[Route("api/[controller]")]
public class ActivityController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;
    /// <summary>Creates the authenticated activity controller.</summary>
    public ActivityController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser) : base(mediator, mapper) => _currentUser = currentUser;
    /// <summary>Returns account-scoped counts across all pages and filters.</summary>
    [HttpGet("summary")]
    public Task<IActionResult> Summary() => ProcessAuthenticatedQueryAsync<GetActivitySummaryRequest, ActivitySummaryResult, GetActivitySummaryQuery, ActivitySummaryResponse>(
        new(), _currentUser, (request, userId) => request.UserId = userId);
}
