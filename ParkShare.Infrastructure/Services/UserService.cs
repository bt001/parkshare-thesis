using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    /// <summary>
    /// Provides user persistence operations.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ParkShareDbContext _db;
        private readonly IMapper _mapper;

        /// <summary>
        /// Creates the user service.
        /// </summary>
        /// <param name="db">Database context.</param>
        /// <param name="mapper">Mapper instance.</param>
        public UserService(ParkShareDbContext db, IMapper mapper)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<UserResponse>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var persistenceResult = await _db.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (persistenceResult == null)
                return ServiceResult<UserResponse>.WithError($"User with email {email} not found.");

            return ServiceResult<UserResponse>.Success(_mapper.Map<UserResponse>(persistenceResult));
        }
    }
}
