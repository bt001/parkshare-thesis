namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Listing photo.
    /// Stored as a URL or base64.
    /// </summary>
    public class ListingPhoto
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }

        /// <summary>
        /// Parent listing.
        /// </summary>
        public Listing Listing { get; set; } = default!;

        /// <summary>
        /// Photo URL or path.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Photo as base64 string (optional).
        /// </summary>
        public string? Base64Content { get; set; } = default!;

        /// <summary>
        /// Upload date.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
