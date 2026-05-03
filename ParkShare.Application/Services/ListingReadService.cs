using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class ListingReadService : IListingReadService
    {
        private readonly IListingRepository _listingRepository;

        /// <summary>
        /// Creates the listing read application service.
        /// </summary>
        /// <param name="listingRepository">Listing repository.</param>
        public ListingReadService(IListingRepository listingRepository)
            => _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));

        /// <inheritdoc/>
        public async Task<ServiceResult<ListingCore>> GetCoreAsync(long listingId, CancellationToken ct)
        {
            var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
            if (listing is null)
                return ServiceResult<ListingCore>.WithError("Listing not found.");

            return ServiceResult<ListingCore>.Success(new ListingCore
            {
                Id = listing.Id,
                OwnerId = listing.OwnerId,
                IsActive = listing.IsActive,
                IsAlwaysAvailable = listing.IsAlwaysAvailable
            });
        }
    }
}
