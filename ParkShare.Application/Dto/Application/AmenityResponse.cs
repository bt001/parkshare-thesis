namespace ParkShare.Application.Dto.Application
{
    /// <summary>A selectable parking amenity.</summary>
    public class AmenityResponse
    {
        /// <summary>Catalog identifier.</summary>
        public long Id { get; set; }
        /// <summary>Display name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Optional description.</summary>
        public string? Description { get; set; }
    }
}
