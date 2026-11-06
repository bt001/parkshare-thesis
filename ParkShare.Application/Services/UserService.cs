using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// Creates the user application service.
        /// </summary>
        /// <param name="userRepository">User repository.</param>
        public UserService(IUserRepository userRepository, ICurrentUserService currentUser, ILogger<UserService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetUserByEmailResponse>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                    return ServiceResult<GetUserByEmailResponse>.WithError("User profile is unavailable.");
                var user = await _userRepository.GetByIdWithRolesAsync(_currentUser.UserId.Value, cancellationToken);
                if (user == null || !user.IsActive || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                    return ServiceResult<GetUserByEmailResponse>.WithError("User profile is unavailable.");

                var orderedRoleNames = user.UserRoles
                    .OrderBy(userRole => userRole.RoleId)
                    .Select(userRole => userRole.Role.Name)
                    .ToArray();

                return ServiceResult<GetUserByEmailResponse>.Success(new GetUserByEmailResponse
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = orderedRoleNames.ToCanonicalRoleString(),
                    Roles = orderedRoleNames,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError("User profile lookup failed ({ExceptionType}).", ex.GetType().Name);
                return ServiceResult<GetUserByEmailResponse>.WithError("User profile could not be loaded. Please try again.");
            }
        }
    }
}
