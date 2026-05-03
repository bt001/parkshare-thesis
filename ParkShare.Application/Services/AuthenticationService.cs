using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRepositoryTransactionCoordinator _transactionCoordinator;
        private readonly IJwtTokenService _jwtTokenService;

        /// <summary>
        /// Creates the authentication application service.
        /// </summary>
        public AuthenticationService(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IRepositoryTransactionCoordinator transactionCoordinator,
            IJwtTokenService jwtTokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _transactionCoordinator = transactionCoordinator ?? throw new ArgumentNullException(nameof(transactionCoordinator));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> RegisterUser(
            string userName,
            string firstName,
            string lastName,
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
                return ServiceResult<LoginResponse>.WithError("An account with this email already exists.");

            if (await _userRepository.ExistsByUserNameAsync(userName, cancellationToken))
                return ServiceResult<LoginResponse>.WithError("An account with this username already exists.");

            await using var transaction = await _transactionCoordinator.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = new User
                {
                    UserName = userName,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = password,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _userRepository.AddAsync(user, cancellationToken);
                await _userRoleRepository.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = (long)Roles.Renter
                }, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var createdUser = await _userRepository.GetByIdWithRolesAsync(user.Id, cancellationToken);
                return createdUser is null
                    ? ServiceResult<LoginResponse>.WithError("Failed to load the registered user.")
                    : ServiceResult<LoginResponse>.Success(BuildLoginResponse(createdUser));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ServiceResult<LoginResponse>.WithError(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailWithRolesAsync(email, cancellationToken);

            if (user == null || user.PasswordHash != password)
                return ServiceResult<LoginResponse>.WithError("Invalid email or password.");

            if (!user.IsActive)
                return ServiceResult<LoginResponse>.WithError("User account is inactive.");

            return ServiceResult<LoginResponse>.Success(BuildLoginResponse(user));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken)
        {
            var userId = _jwtTokenService.GetUserIdFromRefreshToken(refreshToken);
            if (!userId.HasValue)
                return ServiceResult<LoginResponse>.WithError("Invalid refresh token.");

            var user = await _userRepository.GetByIdWithRolesAsync(userId.Value, cancellationToken);
            if (user is null || !user.IsActive)
                return ServiceResult<LoginResponse>.WithError("Invalid refresh token.");

            return ServiceResult<LoginResponse>.Success(BuildLoginResponse(user));
        }

        /// <summary>
        /// Maps a domain user into the authenticated login response.
        /// </summary>
        private LoginResponse BuildLoginResponse(User user)
        {
            var orderedRoleNames = user.UserRoles
                .OrderBy(userRole => userRole.RoleId)
                .Select(userRole => userRole.Role.Name)
                .ToArray();

            var tokens = _jwtTokenService.CreateTokenPair(user);

            return new LoginResponse
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = orderedRoleNames.ToCanonicalRoleString(),
                Roles = orderedRoleNames,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                AccessToken = tokens.AccessToken,
                AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
                TokenType = tokens.TokenType,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
            };
        }
    }
}
