using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Role lookup value.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Role name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Users assigned to the role.
        /// </summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
