using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    /// <summary>
    /// Creates a new user account and returns an authenticated session.
    /// </summary>
    public class RegisterCommand : IRequest<ServiceResult<LoginResponse>>
    {
        /// <summary>
        /// Requested unique username.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

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
    /// Handles user registration requests.
    /// </summary>
    public class RegisterHandler : IRequestHandler<RegisterCommand, ServiceResult<LoginResponse>>
    {
        private readonly IAuthenticationService _authService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        public RegisterHandler(IAuthenticationService authService)
            => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        /// <inheritdoc/>
        public async Task<ServiceResult<LoginResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterUser(
                request.UserName,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                cancellationToken);

            if (!result.IsSuccessful)
                return ServiceResult<LoginResponse>.WithErrors(result.Errors);

            return ServiceResult<LoginResponse>.Success(result.Payload);
        }
    }
}
