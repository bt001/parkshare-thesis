using System;
using System.Collections.Generic;
using System.Linq;

namespace ParkShare.Common.Enums
{
    /// <summary>
    /// Provides ordering and claim helpers for user roles.
    /// </summary>
    public static class RoleCapabilityExtensions
    {
        private static readonly Roles[] OrderedRoles = [Roles.Renter, Roles.Owner, Roles.Admin];

        /// <summary>
        /// Returns the roles in canonical display and serialization order.
        /// </summary>
        /// <returns>Ordered role values.</returns>
        public static IReadOnlyCollection<Roles> GetOrderedRoles() => OrderedRoles;

        /// <summary>
        /// Returns the seeded role names in canonical order.
        /// </summary>
        /// <returns>Ordered role names.</returns>
        public static string[] GetOrderedRoleNames()
            => OrderedRoles.Select(role => role.ToString()).ToArray();

        /// <summary>
        /// Returns the canonical compatibility role string for API responses.
        /// </summary>
        /// <param name="roleNames">Assigned role names.</param>
        /// <returns>Canonical comma-separated role string.</returns>
        public static string ToCanonicalRoleString(this IEnumerable<string> roleNames)
        {
            var assignedNames = new HashSet<string>(roleNames.Where((name) => !string.IsNullOrWhiteSpace(name)), StringComparer.OrdinalIgnoreCase);

            return string.Join(",",
                OrderedRoles
                    .Select(role => role.ToString())
                    .Where(assignedNames.Contains));
        }

        /// <summary>
        /// Converts assigned role names into capability flags.
        /// </summary>
        /// <param name="roleNames">Assigned role names.</param>
        /// <returns>Capability flags.</returns>
        public static Roles ToCapabilities(this IEnumerable<string> roleNames)
        {
            var capabilities = Roles.None;
            foreach (var roleName in roleNames)
            {
                if (Enum.TryParse<Roles>(roleName, true, out var parsedRole) && parsedRole != Roles.None)
                    capabilities |= parsedRole;
            }

            return capabilities;
        }
    }
}
