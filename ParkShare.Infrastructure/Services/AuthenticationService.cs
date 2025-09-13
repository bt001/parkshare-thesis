using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ParkShareDbContext _db;
        private readonly IMapper _mapper;
        public AuthenticationService(ParkShareDbContext db, IMapper mapper)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<UserResponse>> AuthenticateUser(string email, string password, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (user == null || user.PasswordHash != password)
                return ServiceResult<UserResponse>.WithError("Invalid email or password.");

            return ServiceResult<UserResponse>.Success(_mapper.Map<UserResponse>(user));
        }

    }
}
