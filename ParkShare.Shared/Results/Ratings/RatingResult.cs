namespace ParkShare.Shared.Results.Ratings
{
    /// <summary>Saved review without private booking or renter identifiers.</summary>
    public class RatingResult : IApiResult
    {
        /// <summary>Review identity.</summary>
        public long Id { get; set; }
        /// <summary>Stars from one to five.</summary>
        public int Stars { get; set; }
        /// <summary>Plain-text review.</summary>
        public string Comment { get; set; } = string.Empty;
        /// <summary>Original creation time in UTC.</summary>
        public DateTime CreatedAt { get; set; }
    }
}
