using ParkShare.Common.Enums;
using ParkShare.Shared.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Application.Dto.Application
{
    public class BookingResponse
    {
        public long BookingId { get; set; }
        public long ListingId { get; set; }
        public long RenterId { get; set; }
        public long OwnerId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public DateTime CreatedAt { get; set; }
    }
}
