using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ParkShareDbContext _db;
        private readonly IMapper _mapper;
        public UserService(ParkShareDbContext db, IMapper mapper)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<UserResponse>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var persistenceResult = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (persistenceResult == null)
                return ServiceResult<UserResponse>.WithError($"User with email {email} not found.");

            return ServiceResult<UserResponse>.Success(_mapper.Map<UserResponse>(persistenceResult));
        }
    }
}
