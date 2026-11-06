using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    /// <summary>Converts legacy credentials before the API accepts requests.</summary>
    public sealed class PasswordMigrationInitializer
    {
        private readonly ParkShareDbContext _db;
        private readonly IPasswordService _passwords;
        private readonly ILogger<PasswordMigrationInitializer> _logger;

        /// <summary>Creates the startup credential converter.</summary>
        public PasswordMigrationInitializer(ParkShareDbContext db, IPasswordService passwords,
            ILogger<PasswordMigrationInitializer> logger)
        {
            _db = db;
            _passwords = passwords;
            _logger = logger;
        }

        /// <summary>Atomically hashes legacy credentials; failures prevent startup and allow retry.</summary>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                var users = await _db.Users
                    .FromSqlRaw("SELECT * FROM [Users] WITH (TABLOCKX, HOLDLOCK) WHERE [IsPasswordHashed] = 0")
                    .ToListAsync(cancellationToken);
                foreach (var user in users)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    user.PasswordHash = _passwords.Hash(user, user.PasswordHash);
                    user.IsPasswordHashed = true;
                }
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Credential conversion completed for {Count} accounts.", users.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                // Database exceptions can contain statement values; never log credential details.
                _logger.LogError("Credential conversion failed. API startup has been stopped.");
                throw new InvalidOperationException("Credential conversion failed. Retry startup after checking database availability.");
            }
        }
    }
}
