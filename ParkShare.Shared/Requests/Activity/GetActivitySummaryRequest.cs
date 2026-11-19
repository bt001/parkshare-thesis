using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Activity;
/// <summary>GetActivitySummaryRequest API contract.</summary>
public class GetActivitySummaryRequest : IApiRequest
{
    /// <summary>Authenticated identity assigned by the API.</summary>
    public long UserId { get; set; }
}
