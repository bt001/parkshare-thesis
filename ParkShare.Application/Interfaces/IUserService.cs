using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Returns a user profile by email.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>User profile when found.</returns>
        Task<ServiceResult<GetUserByEmailResponse>> GetUserByEmail(string email, CancellationToken cancellationToken);
    }
}
