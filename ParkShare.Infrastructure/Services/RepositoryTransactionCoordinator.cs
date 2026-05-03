using Microsoft.EntityFrameworkCore.Storage;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    /// <inheritdoc/>
    public class RepositoryTransactionCoordinator : IRepositoryTransactionCoordinator
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the repository transaction coordinator.
        /// </summary>
        /// <param name="db">Database context.</param>
        public RepositoryTransactionCoordinator(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public async Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
            => new RepositoryTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

        /// <summary>
        /// Adapts an EF Core transaction to the application abstraction.
        /// </summary>
        private sealed class RepositoryTransaction : IRepositoryTransaction
        {
            private readonly IDbContextTransaction _transaction;

            /// <summary>
            /// Creates the repository transaction wrapper.
            /// </summary>
            public RepositoryTransaction(IDbContextTransaction transaction)
                => _transaction = transaction;

            /// <inheritdoc/>
            public Task CommitAsync(CancellationToken cancellationToken)
                => _transaction.CommitAsync(cancellationToken);

            /// <inheritdoc/>
            public Task RollbackAsync(CancellationToken cancellationToken)
                => _transaction.RollbackAsync(cancellationToken);

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
                => _transaction.DisposeAsync();
        }
    }
}
