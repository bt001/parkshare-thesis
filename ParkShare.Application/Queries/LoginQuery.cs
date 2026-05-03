using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    /// <summary>
    /// Authenticates a user by email and password.
    /// </summary>
    public class LoginQuery : IRequest<ServiceResult<LoginResponse>>
    {
        /// <summary>
        /// User email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handles login requests.
    /// </summary>
    public class LoginHandler : IRequestHandler<LoginQuery, ServiceResult<LoginResponse>>
    {
        private readonly IAuthenticationService _authService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        public LoginHandler(IAuthenticationService authService)
            => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.AuthenticateUser(request.Email, request.Password, cancellationToken);

            if (!result.IsSuccessful)
                return ServiceResult<LoginResponse>.WithErrors(result.Errors);

            return ServiceResult<LoginResponse>.Success(result.Payload);
        }
    }
}
