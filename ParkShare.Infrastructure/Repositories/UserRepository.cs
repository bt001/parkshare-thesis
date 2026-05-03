using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class UserRepository : IUserRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the user repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public UserRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public Task<bool> ExistsByIdAsync(long userId, CancellationToken cancellationToken)
            => _db.Users.AsNoTracking().AnyAsync(user => user.Id == userId, cancellationToken);

        /// <inheritdoc/>
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
            => _db.Users.AsNoTracking().AnyAsync(user => user.Email == email, cancellationToken);

        /// <inheritdoc/>
        public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken)
            => _db.Users.AsNoTracking().AnyAsync(user => user.UserName == userName, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken)
            => _db.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        /// <inheritdoc/>
        public Task<User?> GetByIdWithRolesAsync(long userId, CancellationToken cancellationToken)
            => _db.Users
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }
}
