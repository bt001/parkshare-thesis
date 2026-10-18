namespace ParkShare.Application.Dto.Application
{
    /// <summary>Resolved geographic candidates for an explicit place lookup.</summary>
    public class SearchPlacesResponse
    {
        /// <summary>Candidates ordered by provider relevance.</summary>
        public IReadOnlyList<PlaceResponse> Items { get; set; } = [];
    }
    /// <summary>A labelled geographic point returned by a location provider.</summary>
    public class PlaceResponse
    {
        /// <summary>Human-readable name and address.</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>WGS84 latitude.</summary>
        public double Latitude { get; set; }
        /// <summary>WGS84 longitude.</summary>
        public double Longitude { get; set; }
    }
}
