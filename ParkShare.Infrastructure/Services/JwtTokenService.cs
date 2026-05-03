using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ParkShare.Infrastructure.Services
{
    /// <inheritdoc/>
    public class JwtTokenService : IJwtTokenService
    {
        private const string AccessTokenType = "access";
        private const string RefreshTokenType = "refresh";
        private const string TokenTypeClaim = "token_type";
        private readonly JwtConfig _jwtConfig;

        /// <summary>
        /// Creates the JWT token service.
        /// </summary>
        /// <param name="jwtOptions">JWT configuration options.</param>
        public JwtTokenService(IOptions<JwtConfig> jwtOptions)
            => _jwtConfig = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));

        /// <inheritdoc/>
        public TokenPair CreateTokenPair(User user)
        {
            var accessToken = CreateToken(user, AccessTokenType, DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes));
            var refreshToken = CreateToken(user, RefreshTokenType, DateTime.UtcNow.AddDays(_jwtConfig.RefreshExpirationDays));

            return new TokenPair
            {
                AccessToken = accessToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                TokenType = "Bearer",
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        /// <inheritdoc/>
        public long? GetUserIdFromRefreshToken(string refreshToken)
        {
            var principal = ValidateRefreshToken(refreshToken);
            var userIdValue = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdValue, out var userId) ? userId : null;
        }

        /// <summary>
        /// Creates a signed JWT for the authenticated user.
        /// </summary>
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
