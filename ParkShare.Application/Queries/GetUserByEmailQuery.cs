using AutoMapper;
using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class GetUserByEmailQuery : IRequest<ServiceResult<GetUserByEmailResponse>>
    {
        /// <summary>
        /// Email address associated with the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }

    public class GetUserByEmailHandler : IRequestHandler<GetUserByEmailQuery, ServiceResult<GetUserByEmailResponse>>
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetUserByEmailHandler(IUserService userService, IMapper mapper)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<GetUserByEmailResponse>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
        {
            var response = await _userService.GetUserByEmail(request.Email, cancellationToken);

            if (!response.IsSuccessful)         
                return ServiceResult<GetUserByEmailResponse>.WithErrors(response.Errors);
          
            return ServiceResult<GetUserByEmailResponse>.Success(_mapper.Map<GetUserByEmailResponse>(response.Payload));
        }
    }
}
