namespace ParkShare.Domain.Entities.JoinModels
{
    /// <summary>
    /// Listing-amenity association.
    /// Many-to-many join table.
    /// </summary>
    public class ListingAmenity
    {
        /// <summary>
        /// Listing ID (FK).
        /// </summary>
        public long ListingId { get; set; }

        /// <summary>
        /// Amenity ID (FK).
        /// </summary>
        public long AmenityId { get; set; }

        /// <summary>
        /// Parent listing.
        /// </summary>
        public Listing Listing { get; set; } = default!;

        /// <summary>
        /// Linked amenity.
        /// </summary>
        public Amenity Amenity { get; set; } = default!;
    }
}
