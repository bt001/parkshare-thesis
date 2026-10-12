namespace ParkShare.Shared.Results.Amenities
{
    /// <summary>Available amenities ordered by name.</summary>
    public class GetAmenitiesResult : IApiResult
    {
        /// <summary>Catalog entries.</summary>
        public List<AmenityResult> Items { get; set; } = new();
    }
}
