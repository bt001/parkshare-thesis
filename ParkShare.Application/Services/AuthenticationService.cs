using Microsoft.Extensions.Logging;
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
        private readonly IPasswordService _passwords;
        private readonly ILogger<AuthenticationService> _logger;

        /// <summary>
        /// Creates the authentication application service.
        /// </summary>
        public AuthenticationService(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IRepositoryTransactionCoordinator transactionCoordinator,
            IJwtTokenService jwtTokenService,
            IPasswordService passwords,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _transactionCoordinator = transactionCoordinator ?? throw new ArgumentNullException(nameof(transactionCoordinator));
            _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
            _passwords = passwords ?? throw new ArgumentNullException(nameof(passwords));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            return await ExecuteAsync(async () =>
            {
                if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
                    return ServiceResult<LoginResponse>.WithError("An account with this email already exists.");

                if (await _userRepository.ExistsByUserNameAsync(userName, cancellationToken))
                    return ServiceResult<LoginResponse>.WithError("An account with this username already exists.");

                await using var transaction = await _transactionCoordinator.BeginTransactionAsync(cancellationToken);
                var user = new User
                {
                    UserName = userName,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    IsPasswordHashed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                user.PasswordHash = _passwords.Hash(user, password);
                await _userRepository.AddAsync(user, cancellationToken);
                await _userRoleRepository.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = (long)Roles.Renter
                }, cancellationToken);

                var createdUser = await _userRepository.GetByIdWithRolesAsync(user.Id, cancellationToken)
                    ?? throw new InvalidOperationException("Registered account could not be loaded.");
                var response = BuildLoginResponse(createdUser);
                await transaction.CommitAsync(cancellationToken);
                return ServiceResult<LoginResponse>.Success(response);
            }, "Registration", cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var user = await _userRepository.GetByEmailWithRolesAsync(email, cancellationToken);
                if (user == null || !_passwords.Verify(user, password, out var needsRehash))
                    return ServiceResult<LoginResponse>.WithError("Invalid email or password.");
                if (!user.IsActive)
                    return ServiceResult<LoginResponse>.WithError("User account is inactive.");
                if (needsRehash)
                    await _userRepository.UpdatePasswordHashAsync(user.Id, _passwords.Hash(user, password), cancellationToken);
                return ServiceResult<LoginResponse>.Success(BuildLoginResponse(user));
            }, "Sign in", cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var userId = _jwtTokenService.GetUserIdFromRefreshToken(refreshToken);
                if (!userId.HasValue)
                    return ServiceResult<LoginResponse>.WithError("Invalid refresh token.");
                var user = await _userRepository.GetByIdWithRolesAsync(userId.Value, cancellationToken);
                if (user is null || !user.IsActive)
                    return ServiceResult<LoginResponse>.WithError("Invalid refresh token.");
                return ServiceResult<LoginResponse>.Success(BuildLoginResponse(user));
            }, "Session refresh", cancellationToken);
        }

        /// <summary>Returns safe operational errors while preserving caller cancellation.</summary>
        private async Task<ServiceResult<LoginResponse>> ExecuteAsync(
            Func<Task<ServiceResult<LoginResponse>>> action, string operation, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await action();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Exception messages can include database values; log only the failure type.
                _logger.LogError("{Operation} failed ({ExceptionType}).", operation, ex.GetType().Name);
                return ServiceResult<LoginResponse>.WithError($"{operation} could not be completed. Please try again.");
            }
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
