namespace ParkShare.Shared.Results.Listings
{
    public class GetListingByIdResult : IApiResult
    {
        public long Id { get; set; }
        public long OwnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Size { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
        public bool IsAlwaysAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
