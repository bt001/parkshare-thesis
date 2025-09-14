using ParkShare.Application.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task<ServiceResult<bool>> HasWindowCoverageAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct);
        Task<ServiceResult<bool>> HasBlockedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct);
    }
}
