using ParkShare.Domain.Entities.JoinModels;

namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Parking space listing.
    /// Owned and managed by a user.
    /// </summary>
    public class Listing
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Owner user ID.
        /// </summary>
        public long OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        /// <summary>
        /// Listing title.
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// Listing description.
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Street address (optional).
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Latitude (WGS84).
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Longitude (WGS84).
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Space size or type.
        /// </summary>
        public string? Size { get; set; }

        /// <summary>
        /// Price per hour.
        /// </summary>
        public decimal PricePerHour { get; set; }

        /// <summary>
        /// Listing is visible and active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// If true, listing is always available.
        /// </summary>
        public bool IsAlwaysAvailable { get; set; } = true;

        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
        public ICollection<ListingAmenity> ListingAmenities { get; set; } = new List<ListingAmenity>();
        public ICollection<AvailabilityWindow> AvailabilityWindows { get; set; } = new List<AvailabilityWindow>();
        public ICollection<Booking> BookingRequests { get; set; } = new List<Booking>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
