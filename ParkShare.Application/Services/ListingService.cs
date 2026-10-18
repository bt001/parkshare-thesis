using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class ListingService : IListingService
    {
        private readonly IAmenityRepository _amenityRepository;
        private readonly IListingAmenityRepository _listingAmenityRepository;
        private readonly ILogger<ListingService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IRepositoryTransactionCoordinator _transactionCoordinator;

        /// <summary>
        /// Creates the listing application service.
        /// </summary>
        public ListingService(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IListingRepository listingRepository,
            IRepositoryTransactionCoordinator transactionCoordinator, ILogger<ListingService> logger,
            IAmenityRepository amenityRepository, IListingAmenityRepository listingAmenityRepository)
        {
            _amenityRepository = amenityRepository;
            _listingAmenityRepository = listingAmenityRepository;
            _logger = logger;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
            _transactionCoordinator = transactionCoordinator ?? throw new ArgumentNullException(nameof(transactionCoordinator));
        }

        /// <summary>Validates selections against the catalog without silently dropping invalid identifiers.</summary>
        private async Task<bool> ValidAmenitiesAsync(IReadOnlyCollection<long>? ids, CancellationToken ct)
        {
            if (ids is null || ids.Count == 0) return true;
            if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count) return false;
            var catalog = await _amenityRepository.GetAllAsync(ct);
            var known = catalog.Select(a => a.Id).ToHashSet();
            return ids.All(known.Contains);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateListingResponse>> CreateAsync(long ownerId, string title, string description, decimal pricePerHour, double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct, IReadOnlyCollection<long>? amenityIds = null)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(ownerId, ct))
                    return ServiceResult<CreateListingResponse>.WithError("Owner not found.");
                await using var transaction = await _transactionCoordinator.BeginTransactionAsync(ct);
                if (!await ValidAmenitiesAsync(amenityIds, ct))
                    return ServiceResult<CreateListingResponse>.WithError("Select valid, unique amenity IDs.");
                var ownerRoleId = (long)Roles.Owner;
                if (!await _userRoleRepository.ExistsAsync(ownerId, ownerRoleId, ct))
                {
                    await _userRoleRepository.AddAsync(new UserRole
                    {
                        UserId = ownerId,
                        RoleId = ownerRoleId
                    }, ct);
                }

                var listing = new Listing
                {
                    OwnerId = ownerId,
                    Title = title,
                    Description = description,
                    Address = address,
                    Size = size,
                    Latitude = (decimal)latitude,
                    Longitude = (decimal)longitude,
                    PricePerHour = pricePerHour,
                    IsAlwaysAvailable = isAlwaysAvailable,
                    IsActive = true
                };

                await _listingRepository.AddAsync(listing, ct);
                await _listingRepository.LockAsync(listing.Id, ct);
                await _listingAmenityRepository.ReplaceAsync(listing.Id, amenityIds ?? Array.Empty<long>(), ct);
                await transaction.CommitAsync(ct);

                return ServiceResult<CreateListingResponse>.Success(new CreateListingResponse
                {
                    ListingId = listing.Id,
                    IsActive = listing.IsActive
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing creation failed for owner {OwnerId}.", ownerId);
                return ServiceResult<CreateListingResponse>.WithError("The listing could not be created. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetListingByIdResponse>> GetByIdAsync(long id, CancellationToken ct)
        {
            try
            {
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(id, ct);
                if (listing is null)
                    return ServiceResult<GetListingByIdResponse>.WithError("Listing not found.");

                return ServiceResult<GetListingByIdResponse>.Success(new GetListingByIdResponse
                {
                    Id = listing.Id,
                    OwnerId = listing.OwnerId,
                    Title = listing.Title,
                    Description = listing.Description,
                    Address = listing.Address,
                    Latitude = listing.Latitude,
                    Longitude = listing.Longitude,
                    Size = listing.Size,
                    PricePerHour = listing.PricePerHour,
                    IsActive = listing.IsActive,
                    IsAlwaysAvailable = listing.IsAlwaysAvailable,
                    CreatedAt = listing.CreatedAt,
                    Amenities = (await _listingAmenityRepository.GetByListingAsync(id, ct))
                        .Select(a => new AmenityResponse { Id = a.Id, Name = a.Name, Description = a.Description }).ToList()
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing details could not be loaded for listing {ListingId}.", id);
                return ServiceResult<GetListingByIdResponse>.WithError("The listing could not be loaded. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetOwnerListingsResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct)
        {
            if (!await _userRepository.ExistsByIdAsync(ownerId, ct))
                return ServiceResult<GetOwnerListingsResponse>.WithError("Owner not found.");

            var listings = await _listingRepository.GetByOwnerAsync(ownerId, ct);
            return ServiceResult<GetOwnerListingsResponse>.Success(new GetOwnerListingsResponse
            {
                Items = listings
                    .Select(listing => new ListingSummaryDto
                    {
                        Id = listing.Id,
                        Title = listing.Title,
                        Address = listing.Address,
                        PricePerHour = listing.PricePerHour,
                        Latitude = listing.Latitude,
                        Longitude = listing.Longitude,
                        IsActive = listing.IsActive,
                        CreatedAt = listing.CreatedAt
                    })
                    .ToList()
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<UpdateListingResponse>> UpdateAsync(long id, long ownerId, string title, string description, decimal pricePerHour, double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct, IReadOnlyCollection<long>? amenityIds = null)
        {
            try
            {
                await using var transaction = await _transactionCoordinator.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(id, ct);
                var listing = await _listingRepository.GetByIdAsync(id, ct);
                if (listing is null)
                    return ServiceResult<UpdateListingResponse>.WithError("Listing not found.");

                if (listing.OwnerId != ownerId)
                    return ServiceResult<UpdateListingResponse>.WithError("Not authorized to update this listing.");

                if (!await ValidAmenitiesAsync(amenityIds, ct))
                    return ServiceResult<UpdateListingResponse>.WithError("Select valid, unique amenity IDs.");

                listing.Title = title;
                listing.Description = description;
                listing.PricePerHour = pricePerHour;
                listing.Latitude = (decimal)latitude;
                listing.Longitude = (decimal)longitude;
                listing.Address = address;
                listing.Size = size;
                listing.IsAlwaysAvailable = isAlwaysAvailable;

                if (amenityIds is not null)
                    await _listingAmenityRepository.ReplaceAsync(id, amenityIds, ct);
                await _listingRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<UpdateListingResponse>.Success(new UpdateListingResponse
                {
                    ListingId = listing.Id,
                    IsActive = listing.IsActive
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing mutation failed for listing {ListingId}.", id);
                return ServiceResult<UpdateListingResponse>.WithError("The listing could not be saved. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<DeactivateListingResponse>> DeactivateAsync(long id, long ownerId, CancellationToken ct)
        {
            try
            {
                await using var transaction = await _transactionCoordinator.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(id, ct);
                var listing = await _listingRepository.GetByIdAsync(id, ct);
                if (listing is null)
                    return ServiceResult<DeactivateListingResponse>.WithError("Listing not found.");

                if (listing.OwnerId != ownerId)
                    return ServiceResult<DeactivateListingResponse>.WithError("Not authorized to deactivate this listing.");

                if (!listing.IsActive)
                    return ServiceResult<DeactivateListingResponse>.Success(new DeactivateListingResponse
                    {
                        ListingId = listing.Id,
                        IsActive = listing.IsActive
                    });

                listing.IsActive = false;

                await _listingRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<DeactivateListingResponse>.Success(new DeactivateListingResponse
                {
                    ListingId = listing.Id,
                    IsActive = listing.IsActive
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing mutation failed for listing {ListingId}.", id);
                return ServiceResult<DeactivateListingResponse>.WithError("The listing could not be saved. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<ActivateListingResponse>> ActivateAsync(long id, long ownerId, CancellationToken ct)
        {
            try
            {
                await using var transaction = await _transactionCoordinator.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(id, ct);
                var listing = await _listingRepository.GetByIdAsync(id, ct);
                if (listing is null)
                    return ServiceResult<ActivateListingResponse>.WithError("Listing not found.");

                if (listing.OwnerId != ownerId)
                    return ServiceResult<ActivateListingResponse>.WithError("Not authorized to activate this listing.");

                if (listing.IsActive)
                    return ServiceResult<ActivateListingResponse>.Success(new ActivateListingResponse
                    {
                        ListingId = listing.Id,
                        IsActive = listing.IsActive
                    });

                listing.IsActive = true;

                await _listingRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<ActivateListingResponse>.Success(new ActivateListingResponse
                {
                    ListingId = listing.Id,
                    IsActive = listing.IsActive
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing mutation failed for listing {ListingId}.", id);
                return ServiceResult<ActivateListingResponse>.WithError("The listing could not be saved. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<SearchListingsResponse>> SearchAsync(double? lat, double? lng, double? radiusKm, decimal? minPrice, decimal? maxPrice, bool? onlyActive, bool? onlyAlwaysAvailable, string? titleContains, string? addressContains, int page, int pageSize, CancellationToken ct, DateTime? startDateTime = null, DateTime? endDateTime = null, IReadOnlyCollection<long>? amenityIds = null, double? minRating = null, string? sortBy = null)
        {
            if (startDateTime.HasValue != endDateTime.HasValue)
                return ServiceResult<SearchListingsResponse>.WithError("Provide both arrival and departure.");
            if (startDateTime.HasValue)
            {
                var error = new BookingTimePolicy(TimeProvider.System).Validate(startDateTime.Value, endDateTime!.Value);
                if (error != null) return ServiceResult<SearchListingsResponse>.WithError(error);
            }
            try
            {
                var results = await _listingRepository.SearchAsync(lat, lng, radiusKm, minPrice, maxPrice, onlyActive, onlyAlwaysAvailable, titleContains, addressContains, page, pageSize, ct, startDateTime, endDateTime, amenityIds, minRating, sortBy);

                return ServiceResult<SearchListingsResponse>.Success(new SearchListingsResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = results.TotalCount,
                    Items = results.Items.Select(listing => new SearchListingItemDto
                    {
                        CoverPhotoUrl = listing.CoverPhotoId.HasValue ? $"api/listing/{listing.Id}/photos/{listing.CoverPhotoId}/content" : null,
                        AverageRating = listing.AverageRating,
                        RatingCount = listing.RatingCount,
                        EstimatedPeriodCost = startDateTime.HasValue && endDateTime.HasValue
                            ? Math.Round(listing.PricePerHour * (endDateTime.Value - startDateTime.Value).Ticks / TimeSpan.TicksPerHour, 2, MidpointRounding.AwayFromZero) : null,
                        Id = listing.Id,
                        Title = listing.Title,
                        Address = listing.Address,
                        Latitude = listing.Latitude,
                        Longitude = listing.Longitude,
                        PricePerHour = listing.PricePerHour,
                        IsActive = listing.IsActive,
                        IsAlwaysAvailable = listing.IsAlwaysAvailable,
                        CreatedAt = listing.CreatedAt
                    }).ToList()
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Listing search failed.");
                return ServiceResult<SearchListingsResponse>.WithError("Parking could not be searched. Please try again.");
            }
        }
    }
}
