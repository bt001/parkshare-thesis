using ParkShare.Shared.Results;
namespace ParkShare.Shared.Results.Chat;
/// <summary>ReadMessagesResult API contract.</summary>
public class ReadMessagesResult : IApiResult
{
    /// <summary>Newly acknowledged incoming messages.</summary>
    public int UpdatedCount { get; set; }
}
