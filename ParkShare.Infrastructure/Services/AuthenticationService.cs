using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Domain.Entities.JoinModels;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ParkShare.Infrastructure.Services
{
    /// <inheritdoc/>
    public class AuthenticationService : IAuthenticationService
    {
        private const string AccessTokenType = "access";
        private const string RefreshTokenType = "refresh";
        private const string TokenTypeClaim = "token_type";
        private readonly ParkShareDbContext _db;
        private readonly IMapper _mapper;
        private readonly JwtConfig _jwtConfig;

        /// <summary>
        /// Creates the authentication service.
        /// </summary>
        /// <param name="db">Database context.</param>
        /// <param name="mapper">Mapper instance.</param>
        /// <param name="jwtOptions">JWT options.</param>
        public AuthenticationService(ParkShareDbContext db, IMapper mapper, IOptions<JwtConfig> jwtOptions)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _jwtConfig = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<AuthenticatedUserResponse>> RegisterUser(
            string userName,
            string firstName,
            string lastName,
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            if (await _db.Users.AnyAsync(user => user.Email == email, cancellationToken))
                return ServiceResult<AuthenticatedUserResponse>.WithError("An account with this email already exists.");

            if (await _db.Users.AnyAsync(user => user.UserName == userName, cancellationToken))
                return ServiceResult<AuthenticatedUserResponse>.WithError("An account with this username already exists.");

            var user = new User
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = password,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                UserRoles =
                [
                    new UserRole
                    {
                        RoleId = (long)Roles.Renter
                    }
                ]
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            await _db.Entry(user)
                .Collection(currentUser => currentUser.UserRoles)
                .Query()
                .Include(userRole => userRole.Role)
                .LoadAsync(cancellationToken);

            return ServiceResult<AuthenticatedUserResponse>.Success(CreateAuthenticatedUserResponse(user));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<AuthenticatedUserResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(currentUser => currentUser.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null || user.PasswordHash != password)
                return ServiceResult<AuthenticatedUserResponse>.WithError("Invalid email or password.");

            if (!user.IsActive)
                return ServiceResult<AuthenticatedUserResponse>.WithError("User account is inactive.");

            return ServiceResult<AuthenticatedUserResponse>.Success(CreateAuthenticatedUserResponse(user));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<AuthenticatedUserResponse>> RefreshTokens(string refreshToken, CancellationToken cancellationToken)
        {
            var principal = ValidateRefreshToken(refreshToken);
            if (principal is null)
                return ServiceResult<AuthenticatedUserResponse>.WithError("Invalid refresh token.");

            var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdValue, out var userId))
                return ServiceResult<AuthenticatedUserResponse>.WithError("Invalid refresh token.");

            var user = await _db.Users
                .Include(currentUser => currentUser.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null || !user.IsActive)
                return ServiceResult<AuthenticatedUserResponse>.WithError("Invalid refresh token.");

            return ServiceResult<AuthenticatedUserResponse>.Success(CreateAuthenticatedUserResponse(user));
        }

        /// <summary>
        /// Creates authentication response data for the user.
        /// </summary>
        /// <param name="user">Authenticated user.</param>
        /// <returns>Authenticated user response.</returns>
        private AuthenticatedUserResponse CreateAuthenticatedUserResponse(User user)
        {
            var response = _mapper.Map<AuthenticatedUserResponse>(user);
            var accessToken = CreateToken(user, AccessTokenType, DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes));
            var refreshToken = CreateToken(user, RefreshTokenType, DateTime.UtcNow.AddDays(_jwtConfig.RefreshExpirationDays));

            response.AccessToken = accessToken.Token;
            response.AccessTokenExpiresAt = accessToken.ExpiresAt;
            response.RefreshToken = refreshToken.Token;
            response.RefreshTokenExpiresAt = refreshToken.ExpiresAt;

            return response;
        }

        /// <summary>
        /// Creates a signed JWT for the authenticated user.
        /// </summary>
        /// <param name="user">Authenticated user.</param>
        /// <param name="tokenType">Token type.</param>
        /// <param name="expiresAt">Expiration timestamp.</param>
        /// <returns>Token data.</returns>
        private (string Token, DateTime ExpiresAt) CreateToken(User user, string tokenType, DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(TokenTypeClaim, tokenType)
            };

            claims.AddRange(user.UserRoles
                .OrderBy(userRole => userRole.RoleId)
                .Select(userRole => new Claim(ClaimTypes.Role, userRole.Role.Name)));

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        /// <summary>
        /// Validates a refresh token and returns its principal when valid.
        /// </summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <returns>Claims principal when valid; otherwise <c>null</c>.</returns>
        private ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(refreshToken, CreateTokenValidationParameters(), out var validatedToken);
                if (validatedToken is not JwtSecurityToken jwtToken
                    || !string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                    return null;

                var tokenType = principal.FindFirst(TokenTypeClaim)?.Value;
                return string.Equals(tokenType, RefreshTokenType, StringComparison.Ordinal) ? principal : null;
            }
            catch (SecurityTokenException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates token validation parameters for ParkShare JWTs.
        /// </summary>
        /// <returns>Token validation parameters.</returns>
        private TokenValidationParameters CreateTokenValidationParameters()
            => new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidAudience = _jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SigningKey)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };
    }
}
