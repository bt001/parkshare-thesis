using ParkShare.Common.Enums;

namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Provides the current authenticated user context.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Current user ID when authenticated.
        /// </summary>
        long? UserId { get; }

        /// <summary>
        /// Current user email when authenticated.
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Current user capabilities when authenticated.
        /// </summary>
        Roles Capabilities { get; }

        /// <summary>
        /// Current user capability names when authenticated.
        /// </summary>
        IReadOnlyCollection<string> RoleNames { get; }

        /// <summary>
        /// Whether the current request has an authenticated user.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
