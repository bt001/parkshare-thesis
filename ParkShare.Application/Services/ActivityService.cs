using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
namespace ParkShare.Application.Services;
/// <inheritdoc/>
public class ActivityService : IActivityService
{
    private readonly IActivityRepository _repository;
    private readonly ILogger<ActivityService> _logger;
    /// <summary>Creates the activity service.</summary>
    public ActivityService(IActivityRepository repository, ILogger<ActivityService> logger) { _repository = repository; _logger = logger; }
    /// <inheritdoc/>
    public async Task<ServiceResult<ActivitySummaryResponse>> GetAsync(long userId, CancellationToken ct)
    {
        try { return ServiceResult<ActivitySummaryResponse>.Success(await _repository.GetSummaryAsync(userId, ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { _logger.LogError(ex, "Activity retrieval failed for {UserId}.", userId); return ServiceResult<ActivitySummaryResponse>.WithError("Activity could not be loaded. Please retry."); }
    }
}
