using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    /// <summary>
    /// Refreshes authentication tokens.
    /// </summary>
    public class RefreshTokenQuery : IRequest<ServiceResult<LoginResponse>>
    {
        /// <summary>
        /// JWT refresh token.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handles token refresh requests.
    /// </summary>
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenQuery, ServiceResult<LoginResponse>>
    {
        private readonly IAuthenticationService _authService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        public RefreshTokenHandler(IAuthenticationService authService)
            => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokens(request.RefreshToken, cancellationToken);

            if (!result.IsSuccessful)
                return ServiceResult<LoginResponse>.WithErrors(result);

            return ServiceResult<LoginResponse>.Success(result.Payload);
        }
    }
}
