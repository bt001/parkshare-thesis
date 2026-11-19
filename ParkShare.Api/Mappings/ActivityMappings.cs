using AutoMapper;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Shared.Requests.Activity;
using ParkShare.Shared.Results.Activity;
namespace ParkShare.Api.Mappings;
/// <summary>Maps authenticated activity contracts.</summary>
public class ActivityMappings : IHaveCustomMapping
{
    /// <inheritdoc/>
    public void CreateMappings(Profile cfg)
    {
        cfg.CreateMap<GetActivitySummaryRequest, GetActivitySummaryQuery>();
        cfg.CreateMap<ActivitySummaryResponse, ActivitySummaryResult>();
    }
}
