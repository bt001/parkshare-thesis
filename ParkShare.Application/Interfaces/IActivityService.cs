using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces;
/// <summary>Provides safe authenticated activity summaries.</summary>
public interface IActivityService
{
    /// <summary>Reads the current account's navigation counts.</summary>
    Task<ServiceResult<ActivitySummaryResponse>> GetAsync(long userId, CancellationToken ct);
}
