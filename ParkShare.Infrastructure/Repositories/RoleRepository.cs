using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class RoleRepository : IRoleRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the role repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public RoleRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public Task<Role?> GetByIdAsync(long roleId, CancellationToken cancellationToken)
            => _db.Roles.AsNoTracking().FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }
}
