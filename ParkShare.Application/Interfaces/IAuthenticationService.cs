using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;

namespace ParkShare.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<ServiceResult<UserResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken);
    }
}