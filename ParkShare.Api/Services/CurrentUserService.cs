using Microsoft.AspNetCore.Http;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public Roles Capabilities
            => !IsAuthenticated
                ? Roles.None
                : (_httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role) ?? [])
                    .Select(claim => RoleCapabilityExtensions.ParseCapabilities(claim.Value))
                    .Aggregate(Roles.None, (current, next) => current | next)
                    .NormalizeMarketplaceCapabilities();

        /// <inheritdoc/>
        public IReadOnlyCollection<string> RoleNames => IsAuthenticated ? Capabilities.ToRoleNames() : Array.Empty<string>();

        /// <inheritdoc/>
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    }
}
