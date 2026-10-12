namespace ParkShare.Application.Dto.Application
{
    /// <summary>Available amenities ordered by name.</summary>
    public class GetAmenitiesResponse
    {
        /// <summary>Catalog entries.</summary>
        public List<AmenityResponse> Items { get; set; } = new();
    }
}
