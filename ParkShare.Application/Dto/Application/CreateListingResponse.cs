using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Application.Dto.Application
{
    public class CreateListingResponse
    {
        public long ListingId { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
