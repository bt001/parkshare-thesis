namespace ParkShare.Infrastructure.Configuration
{
    /// <summary>Server-side Geoapify settings; supply the key through secrets or environment configuration.</summary>
    public class PlaceSearchConfig
    {
        /// <summary>Geoapify project key; never sent to the browser.</summary>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>Country restriction for discovery.</summary>
        public string CountryCode { get; set; } = "gr";
        /// <summary>Preferred result language.</summary>
        public string Language { get; set; } = "el";
        /// <summary>Daily outbound request cap per API process, below the free allowance.</summary>
        public int DailyRequestLimit { get; set; } = 2500;
    }
}
