using System;

namespace ParkShare.Common.Enums
{
    /// <summary>
    /// Additive marketplace capabilities for a user account.
    /// </summary>
    [Flags]
    public enum Roles
    {
        None = 0,
        Renter = 1,
        Owner = 2,
        Admin = 4,
    }
}
