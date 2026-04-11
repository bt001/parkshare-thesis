using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class CreateListingRequest : IApiRequest
    {
        /// <summary>
        /// Owner ID, set by the API from the authenticated user.
        /// </summary>
        public long OwnerId { get; set; }

        public string Title { get; set; } = string.Empty;   // 1–120
        public string Description { get; set; } = string.Empty; // ≤2000, required (domain: non-null)
        public decimal PricePerHour { get; set; }           // decimal(10,2) per DbContext

        public double Latitude { get; set; }                // will be cast to decimal in Infra
        public double Longitude { get; set; }
        public string? Address { get; set; }                // ≤200

        // Domain has Size, IsAlwaysAvailable with defaults; we omit from request for now.
    }

}
