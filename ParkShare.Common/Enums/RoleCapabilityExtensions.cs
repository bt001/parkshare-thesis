using System;
using System.Linq;

namespace ParkShare.Common.Enums
{
    /// <summary>
    /// Provides parsing and normalization helpers for user capability flags.
    /// </summary>
    public static class RoleCapabilityExtensions
    {
        private static readonly Roles[] OrderedCapabilities = [Roles.Renter, Roles.Owner, Roles.Admin];
        private static readonly char[] Separators = [',', ';', '|'];

        /// <summary>
        /// Returns the baseline capabilities for a marketplace account.
        /// </summary>
        /// <returns>Baseline capability flags.</returns>
        public static Roles GetDefaultMarketplaceCapabilities() => Roles.Renter;

        /// <summary>
        /// Returns the canonical default capability string for persistence.
        /// </summary>
        /// <returns>Canonical capability string.</returns>
        public static string GetDefaultMarketplaceRoleValue() => GetDefaultMarketplaceCapabilities().ToCanonicalRoleString();

        /// <summary>
        /// Parses persisted role text into additive capability flags.
        /// </summary>
        /// <param name="value">Persisted role text.</param>
        /// <returns>Parsed capabilities.</returns>
        public static Roles ParseCapabilities(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Roles.None;

            if (int.TryParse(value, out var numericValue))
                return (Roles)numericValue;

            var capabilities = Roles.None;
            foreach (var token in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<Roles>(token, true, out var parsedCapability) && parsedCapability != Roles.None)
                    capabilities |= parsedCapability;
            }

            return capabilities;
        }

        /// <summary>
        /// Ensures the account includes the baseline renter capability.
        /// </summary>
        /// <param name="capabilities">Existing capabilities.</param>
        /// <returns>Normalized capabilities.</returns>
        public static Roles NormalizeMarketplaceCapabilities(this Roles capabilities)
            => capabilities | GetDefaultMarketplaceCapabilities();

        /// <summary>
        /// Adds the owner capability while preserving existing capabilities.
        /// </summary>
        /// <param name="capabilities">Existing capabilities.</param>
        /// <returns>Capabilities with owner access.</returns>
        public static Roles AddOwnerCapability(this Roles capabilities)
            => capabilities.NormalizeMarketplaceCapabilities() | Roles.Owner;

        /// <summary>
        /// Converts capabilities to a stable ordered collection of role names.
        /// </summary>
        /// <param name="capabilities">Existing capabilities.</param>
        /// <returns>Ordered capability names.</returns>
        public static string[] ToRoleNames(this Roles capabilities)
            => OrderedCapabilities
                .Where(capability => capabilities.NormalizeMarketplaceCapabilities().HasFlag(capability))
                .Select(capability => capability.ToString())
                .ToArray();

        /// <summary>
        /// Converts capabilities to a canonical persistence and API string.
        /// </summary>
        /// <param name="capabilities">Existing capabilities.</param>
        /// <returns>Canonical capability string.</returns>
        public static string ToCanonicalRoleString(this Roles capabilities)
            => string.Join(",", capabilities.ToRoleNames());
    }
}
