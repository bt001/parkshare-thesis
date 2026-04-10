using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    public class ListingService : IListingService
    {
        private readonly ParkShareDbContext _db;

        public ListingService(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<ServiceResult<CreateListingResponse>> CreateAsync(long ownerId, string title, string description, decimal pricePerHour,
            double latitude, double longitude, string? address, CancellationToken ct)
        {
            // 1) Guard: Owner must exist
            var ownerExists = await _db.Users.AnyAsync(u => u.Id == ownerId, ct);
            if (!ownerExists)
                return ServiceResult<CreateListingResponse>.WithError("Owner not found.");

            var entity = new Listing
            {
                OwnerId = ownerId,
                Title = title,
                Description = description,
                Address = address,
                Latitude = (decimal)latitude,
                Longitude = (decimal)longitude,
                PricePerHour = pricePerHour,
                IsActive = true
            };

            try
            {
                await _db.Listings.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);

                var payload = new CreateListingResponse
                {
                    ListingId = entity.Id,
                    IsActive = entity.IsActive
                };

                return ServiceResult<CreateListingResponse>.Success(payload);
            }
            catch (DbUpdateException ex)
            {
                return ServiceResult<CreateListingResponse>.WithError(ex.Message);
            }
        }

        public async Task<ServiceResult<GetListingByIdResponse>> GetByIdAsync(long id, CancellationToken ct)
        {
            var e = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null)
                return ServiceResult<GetListingByIdResponse>.WithError("Listing not found.");

            var payload = new GetListingByIdResponse 
            { 
                Id = e.Id,
                OwnerId = e.OwnerId,
                Title = e.Title,
                Description = e.Description,
                Address = e.Address,
                Latitude = e.Latitude,
                Longitude = e.Longitude, Size = e.Size,
                PricePerHour = e.PricePerHour,
                IsActive = e.IsActive,
                IsAlwaysAvailable = e.IsAlwaysAvailable,
                CreatedAt = e.CreatedAt 
            };

            return ServiceResult<GetListingByIdResponse>.Success(payload);
        }

        public async Task<ServiceResult<GetOwnerListingsResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct)
        {
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == ownerId, ct);
            if (!exists)
                return ServiceResult<GetOwnerListingsResponse>.WithError("Owner not found.");

            var items = await _db.Listings
              .AsNoTracking()
              .Where(l => l.OwnerId == ownerId)
              .OrderByDescending(l => l.CreatedAt)
              .Select(l => new ListingSummaryDto
              {
                  Id = l.Id,
                  Title = l.Title,
                  Address = l.Address,
                  PricePerHour = l.PricePerHour,
                  Latitude = l.Latitude,
                  Longitude = l.Longitude,
                  IsActive = l.IsActive,
                  CreatedAt = l.CreatedAt
              })
              .ToListAsync(ct);

            var payload = new GetOwnerListingsResponse
            {
                Items = items
            };

            return ServiceResult<GetOwnerListingsResponse>.Success(payload);
        }

        public async Task<ServiceResult<UpdateListingResponse>> UpdateAsync(long id, long ownerId, string title, string description, decimal pricePerHour, double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct)
        {
            var e = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null)
                return ServiceResult<UpdateListingResponse>.WithError("Listing not found.");

            if (e.OwnerId != ownerId)
                return ServiceResult<UpdateListingResponse>.WithError("Not authorized to update this listing.");

            e.Title = title;
            e.Description = description;
            e.PricePerHour = pricePerHour;
            e.Latitude = (decimal)latitude;
            e.Longitude = (decimal)longitude;
            e.Address = address;
            e.Size = size;
            e.IsAlwaysAvailable = isAlwaysAvailable;

            try
            {
                await _db.SaveChangesAsync(ct);
                var payload = new UpdateListingResponse
                {
                    ListingId = e.Id,
                    IsActive = e.IsActive
                };
                return ServiceResult<UpdateListingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<UpdateListingResponse>.WithError("Failed to update listing.");
            }
        }

        public async Task<ServiceResult<DeactivateListingResponse>> DeactivateAsync(long id, long ownerId, CancellationToken ct)
        {
            var e = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null)
                return ServiceResult<DeactivateListingResponse>.WithError("Listing not found.");

            if (e.OwnerId != ownerId)
                return ServiceResult<DeactivateListingResponse>.WithError("Not authorized to deactivate this listing.");

            if (!e.IsActive)
            {
                var already = new DeactivateListingResponse { ListingId = e.Id, IsActive = e.IsActive };
                return ServiceResult<DeactivateListingResponse>.Success(already); // idempotent
            }

            e.IsActive = false;

            try
            {
                await _db.SaveChangesAsync(ct);
                var payload = new DeactivateListingResponse
                {
                    ListingId = e.Id,
                    IsActive = e.IsActive
                };
                return ServiceResult<DeactivateListingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<DeactivateListingResponse>.WithError("Failed to deactivate listing.");
            }
        }

        public async Task<ServiceResult<ActivateListingResponse>> ActivateAsync(long id, long ownerId, CancellationToken ct)
        {
            var e = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null)
                return ServiceResult<ActivateListingResponse>.WithError("Listing not found.");

            if (e.OwnerId != ownerId)
                return ServiceResult<ActivateListingResponse>.WithError("Not authorized to activate this listing.");

            if (e.IsActive)
            {
                var already = new ActivateListingResponse { ListingId = e.Id, IsActive = e.IsActive };
                return ServiceResult<ActivateListingResponse>.Success(already);
            }

            e.IsActive = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                var payload = new ActivateListingResponse { ListingId = e.Id, IsActive = e.IsActive };
                return ServiceResult<ActivateListingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<ActivateListingResponse>.WithError("Failed to activate listing.");
            }
        }

        public async Task<ServiceResult<SearchListingsResponse>> SearchAsync(double? lat, double? lng, double? radiusKm, decimal? minPrice, decimal? maxPrice, bool? onlyActive, bool? onlyAlwaysAvailable, string? titleContains, string? addressContains, int page, int pageSize, CancellationToken ct)
        {
            var query = _db.Listings.AsNoTracking().AsQueryable();

            var active = onlyActive ?? true;
            if (active)
                query = query.Where(l => l.IsActive);

            if (onlyAlwaysAvailable == true)
                query = query.Where(l => l.IsAlwaysAvailable);

            if (minPrice.HasValue)
                query = query.Where(l => l.PricePerHour >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(l => l.PricePerHour <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(titleContains))
                query = query.Where(l => EF.Functions.Like(l.Title, "%" + titleContains + "%"));

            if (!string.IsNullOrWhiteSpace(addressContains))
                query = query.Where(l => l.Address != null && EF.Functions.Like(l.Address, "%" + addressContains + "%"));

            if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            {
                var approxDeg = radiusKm.Value / 111.0;
                var minLat = lat.Value - approxDeg;
                var maxLat = lat.Value + approxDeg;
                var minLng = lng.Value - approxDeg;
                var maxLng = lng.Value + approxDeg;

                query = query.Where(l => (double)l.Latitude >= minLat && (double)l.Latitude <= maxLat && (double)l.Longitude >= minLng && (double)l.Longitude <= maxLng);
            }

            var total = await query.CountAsync(ct);

            var skip = (page - 1) * pageSize;
            var items = await query
              .OrderByDescending(l => l.CreatedAt)
              .Skip(skip)
              .Take(pageSize)
              .Select(l => new SearchListingItemDto
              {
                  Id = l.Id,
                  Title = l.Title,
                  Address = l.Address,
                  Latitude = l.Latitude,
                  Longitude = l.Longitude,
                  PricePerHour = l.PricePerHour,
                  IsActive = l.IsActive,
                  IsAlwaysAvailable = l.IsAlwaysAvailable,
                  CreatedAt = l.CreatedAt
              })
              .ToListAsync(ct);

            var payload = new SearchListingsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };

            return ServiceResult<SearchListingsResponse>.Success(payload);
        }
    }
}
