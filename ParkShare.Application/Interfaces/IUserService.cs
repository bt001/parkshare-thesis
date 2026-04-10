using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;

namespace ParkShare.Application.Interfaces
{
    public interface IUserService
    {
        Task<ServiceResult<UserResponse>> GetUserByEmail(string email, CancellationToken cancellationToken);
    }
}