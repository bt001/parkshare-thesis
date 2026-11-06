using Microsoft.AspNetCore.Identity;
using ParkShare.Application.Interfaces;
using ParkShare.Domain.Entities;

namespace ParkShare.Infrastructure.Services
{
    /// <inheritdoc/>
    public sealed class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<User> _hasher = new();

        /// <inheritdoc/>
        public string Hash(User user, string password) => _hasher.HashPassword(user, password);

        /// <inheritdoc/>
        public bool Verify(User user, string password, out bool needsRehash)
        {
            needsRehash = false;
            if (!user.IsPasswordHashed || string.IsNullOrEmpty(user.PasswordHash))
                return false;
            try
            {
                var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
                needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
                return result != PasswordVerificationResult.Failed;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
