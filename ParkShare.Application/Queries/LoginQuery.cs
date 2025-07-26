using AutoMapper;
using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class LoginQuery : IRequest<ServiceResult<LoginResponse>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginHandler : IRequestHandler<LoginQuery, ServiceResult<LoginResponse>>
    {
        private readonly IAuthenticationService _authService;
        private readonly IMapper _mapper;

        public LoginHandler(IAuthenticationService authService, IMapper mapper)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<LoginResponse>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.AuthenticateUser(request.Email, request.Password, cancellationToken);

            if (!result.IsSuccessful)
                return ServiceResult<LoginResponse>.WithErrors(result.Errors);

            return ServiceResult<LoginResponse>.Success(_mapper.Map<LoginResponse>(result.Payload));
        }
    }
}
