using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class AmenityService : IAmenityService
    {
        private readonly IAmenityRepository _repository;
        private readonly ILogger<AmenityService> _logger;
        /// <summary>Creates the catalog service.</summary>
        public AmenityService(IAmenityRepository repository, ILogger<AmenityService> logger)
        { _repository = repository; _logger = logger; }
        /// <inheritdoc/>
        public async Task<ServiceResult<GetAmenitiesResponse>> GetAllAsync(CancellationToken ct)
        {
            try
            {
                var items = await _repository.GetAllAsync(ct);
                return ServiceResult<GetAmenitiesResponse>.Success(new GetAmenitiesResponse
                {
                    Items = items.Select(a => new AmenityResponse { Id = a.Id, Name = a.Name, Description = a.Description }).ToList()
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Amenity catalog could not be loaded.");
                return ServiceResult<GetAmenitiesResponse>.WithError("Amenities could not be loaded. Please try again.");
            }
        }
    }
}
