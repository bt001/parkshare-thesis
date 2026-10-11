using AutoMapper;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkShare.Application.Commands;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Shared.Requests.Notifications;
using ParkShare.Shared.Results.Notifications;
namespace ParkShare.Api.Controllers
{
    /// <summary>Authenticated recipient inbox endpoints.</summary>
    [Authorize]
    [Route("api/[controller]")]
    public class NotificationController : BaseApiController
    {
        private readonly ICurrentUserService _currentUser;
        /// <summary>Creates the authenticated inbox controller.</summary>
        public NotificationController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser) : base(mediator, mapper)
            => _currentUser = currentUser;

        /// <summary>Returns a page of the authenticated user's notifications.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(GetNotificationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Get([FromQuery] GetNotificationsRequest request)
            => ProcessAuthenticatedQueryAsync<GetNotificationsRequest, GetNotificationsResult, GetNotificationsQuery, GetNotificationsResponse>(
                request, _currentUser, (query, userId) => query.UserId = userId);

        /// <summary>Returns the authenticated user's unread count.</summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(UnreadNotificationCountResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> UnreadCount()
            => ProcessAuthenticatedQueryAsync<GetUnreadNotificationCountRequest, UnreadNotificationCountResult, GetUnreadNotificationCountQuery, UnreadNotificationCountResponse>(
                new(), _currentUser, (query, userId) => query.UserId = userId);

        /// <summary>Marks one recipient-owned notification read.</summary>
        [HttpPatch("{id:long}/read")]
        [ProducesResponseType(typeof(MarkNotificationReadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> MarkRead([FromRoute] long id)
            => ProcessAuthenticatedCommandAsync<MarkNotificationReadRequest, MarkNotificationReadResult, MarkNotificationReadCommand, MarkNotificationReadResponse>(
                new() { Id = id }, _currentUser, (command, userId) => command.UserId = userId);
    }
}
