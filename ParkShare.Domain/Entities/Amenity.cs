using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Listing amenity.
    /// Linked to listings via join table.
    /// </summary>
    public class Amenity
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Amenity name (unique).
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Amenity description.
        /// </summary>
        public string? Description { get; set; }

        public ICollection<ListingAmenity> ListingAmenities { get; set; } = new List<ListingAmenity>();
    }
}
