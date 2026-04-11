using AutoMapper;
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
        private readonly IMapper _mapper;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        /// <param name="mapper">Mapper instance.</param>
        public RefreshTokenHandler(IAuthenticationService authService, IMapper mapper)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Handles the query.
        /// </summary>
        /// <param name="request">Refresh token query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Refreshed authentication data.</returns>
        public async Task<ServiceResult<LoginResponse>> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshTokens(request.RefreshToken, cancellationToken);

            if (!result.IsSuccessful)
                return ServiceResult<LoginResponse>.WithErrors(result.Errors);

            return ServiceResult<LoginResponse>.Success(_mapper.Map<LoginResponse>(result.Payload));
        }
    }
}
