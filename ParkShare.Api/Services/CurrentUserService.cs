using Microsoft.AspNetCore.Http;
using ParkShare.Application.Interfaces;
using System;
using System.Security.Claims;

namespace ParkShare.Api.Services
{
    /// <summary>
    /// Provides current user data from the HTTP context.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates the current user service.
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        /// <inheritdoc/>
        public long? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                return long.TryParse(value, out var userId) ? userId : null;
            }
        }

        /// <inheritdoc/>
        public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

        /// <inheritdoc/>
        public string? Role => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

        /// <inheritdoc/>
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    }
}
