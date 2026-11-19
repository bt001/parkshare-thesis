using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Chat;
/// <summary>ReadMessagesRequest API contract.</summary>
public class ReadMessagesRequest : IApiRequest
{
    /// <summary>Booking assigned from the route.</summary>
    public long BookingId { get; set; }
    /// <summary>Authenticated participant assigned by the API.</summary>
    public long UserId { get; set; }
    /// <summary>Exact displayed incoming message IDs.</summary>
    public List<long>? MessageIds { get; set; }
}
