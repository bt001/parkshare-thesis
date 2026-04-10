using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Shared.Results.Listings
{
    public class UpdateListingResult : IApiResult
    {
        public long ListingId { get; set; }
        public bool IsActive { get; set; }
    }

}
