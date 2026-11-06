using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    /// <summary>
    /// Returns the authenticated user profile for a matching email.
    /// </summary>
    public class GetUserByEmailQuery : IRequest<ServiceResult<GetUserByEmailResponse>>
    {
        /// <summary>
        /// Email address associated with the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handles user-by-email queries.
    /// </summary>
    public class GetUserByEmailHandler : IRequestHandler<GetUserByEmailQuery, ServiceResult<GetUserByEmailResponse>>
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        /// <param name="userService">User service.</param>
        public GetUserByEmailHandler(IUserService userService)
            => _userService = userService ?? throw new ArgumentNullException(nameof(userService));

        /// <inheritdoc/>
        public async Task<ServiceResult<GetUserByEmailResponse>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
        {
            var response = await _userService.GetUserByEmail(request.Email, cancellationToken);

            if (!response.IsSuccessful)
                return ServiceResult<GetUserByEmailResponse>.WithErrors(response);

            return ServiceResult<GetUserByEmailResponse>.Success(response.Payload);
        }
    }
}
