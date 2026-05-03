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

        /// <summary>
        /// Creates the user application service.
        /// </summary>
        /// <param name="userRepository">User repository.</param>
        public UserService(IUserRepository userRepository)
            => _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        /// <inheritdoc/>
        public async Task<ServiceResult<GetUserByEmailResponse>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailWithRolesAsync(email, cancellationToken);
            if (user == null)
                return ServiceResult<GetUserByEmailResponse>.WithError($"User with email {email} not found.");

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
    }
}
