using ParkShare.Domain.Entities;

namespace ParkShare.Domain.Entities.JoinModels
{
    /// <summary>
    /// User-role association.
    /// Many-to-many join table.
    /// </summary>
    public class UserRole
    {
        /// <summary>
        /// User ID (FK).
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Role ID (FK).
        /// </summary>
        public long RoleId { get; set; }

        /// <summary>
        /// Parent user.
        /// </summary>
        public User User { get; set; } = default!;

        /// <summary>
        /// Linked role.
        /// </summary>
        public Role Role { get; set; } = default!;
    }
}
