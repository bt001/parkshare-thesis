using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    /// <summary>Provides the authenticated account profile.</summary>
    public interface IUserService
    {
        /// <summary>
        /// Returns the current authenticated user profile when the requested email matches.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>User profile when found.</returns>
        Task<ServiceResult<GetUserByEmailResponse>> GetUserByEmail(string email, CancellationToken cancellationToken);
    }
}
