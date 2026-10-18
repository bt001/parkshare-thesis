using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Places
{
    /// <summary>Explicit geographic place lookup.</summary>
    public class SearchPlacesRequest : IApiRequest
    {
        /// <summary>City, street address or landmark name.</summary>
        public string Text { get; set; } = string.Empty;
    }
}
