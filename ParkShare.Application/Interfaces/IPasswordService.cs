using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces
{
    /// <summary>Hashes and verifies account passwords without exposing infrastructure formats.</summary>
    public interface IPasswordService
    {
        /// <summary>Creates a salted password hash for the account.</summary>
        string Hash(User user, string password);

        /// <summary>Verifies a password and indicates whether its hash should be upgraded.</summary>
        bool Verify(User user, string password, out bool needsRehash);
    }
}
