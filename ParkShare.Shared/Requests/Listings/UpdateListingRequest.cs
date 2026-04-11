using ParkShare.Shared.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Shared.Requests.Listings
{
    public class UpdateListingRequest : IApiRequest
    {
        public long Id { get; set; }
        /// <summary>
        /// Owner ID, set by the API from the authenticated user.
        /// </summary>
        public long OwnerId { get; set; }
        public string Title { get; set; } = string.Empty;      // 1–120
        public string Description { get; set; } = string.Empty; // ≤2000
        public decimal PricePerHour { get; set; }               // decimal(10,2)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }                    // ≤200
        public string? Size { get; set; }
        public bool IsAlwaysAvailable { get; set; }
    }

}
