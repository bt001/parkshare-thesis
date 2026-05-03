using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities.JoinModels;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the user-role repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public UserRoleRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(long userId, long roleId, CancellationToken cancellationToken)
            => _db.UserRoles.AsNoTracking().AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken)
        {
            await _db.UserRoles.AddAsync(userRole, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
