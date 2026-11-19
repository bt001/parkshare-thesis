namespace ParkShare.Shared.Results.Places
{
    /// <summary>Resolved place candidates.</summary>
    public class SearchPlacesResult : IApiResult
    {
        /// <summary>Ordered geographic matches.</summary>
        public List<PlaceResult> Items { get; set; } = [];
    }
    /// <summary>A place label and geographic centre.</summary>
    public class PlaceResult
    {
        /// <summary>Street name, when supplied by the provider.</summary>
        public string? Street { get; set; }
        /// <summary>Building number matched by the provider.</summary>
        public string? HouseNumber { get; set; }
        /// <summary>City or locality for disambiguation.</summary>
        public string? City { get; set; }
        /// <summary>Postal code for structured lookup.</summary>
        public string? Postcode { get; set; }
        /// <summary>Human-readable name and address.</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>WGS84 latitude.</summary>
        public double Latitude { get; set; }
        /// <summary>WGS84 longitude.</summary>
        public double Longitude { get; set; }
    }
}
