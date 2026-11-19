using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries;
/// <summary>Reads account activity.</summary>
public class GetActivitySummaryQuery : IRequest<ServiceResult<ActivitySummaryResponse>>
{
    /// <summary>Authenticated account identity.</summary>
    public long UserId { get; set; }
}
/// <summary>Delegates activity retrieval to the application service.</summary>
public class GetActivitySummaryHandler : IRequestHandler<GetActivitySummaryQuery, ServiceResult<ActivitySummaryResponse>>
{
    private readonly IActivityService _service;
    /// <summary>Creates the summary handler.</summary>
    public GetActivitySummaryHandler(IActivityService service) => _service = service;
    /// <inheritdoc/>
    public async Task<ServiceResult<ActivitySummaryResponse>> Handle(GetActivitySummaryQuery request, CancellationToken ct)
    {
        var result = await _service.GetAsync(request.UserId, ct);
        if (!result.IsSuccessful) return ServiceResult<ActivitySummaryResponse>.WithErrors(result);
        return ServiceResult<ActivitySummaryResponse>.Success(result.Payload);
    }
}
