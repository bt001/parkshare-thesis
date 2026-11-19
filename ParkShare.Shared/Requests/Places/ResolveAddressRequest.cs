using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Places
{
    /// <summary>A structured owner listing address lookup.</summary>
    public class ResolveAddressRequest : IApiRequest
    {
        /// <summary>Selected street name.</summary>
        public string Street { get; set; } = string.Empty;
        /// <summary>Building number, including suffixes or ranges.</summary>
        public string HouseNumber { get; set; } = string.Empty;
        /// <summary>Selected city or locality.</summary>
        public string? City { get; set; }
        /// <summary>Selected postal code.</summary>
        public string? Postcode { get; set; }
    }
}
