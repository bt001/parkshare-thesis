namespace ParkShare.Shared.Results.Listings
{
    public class GetOwnerListingsResult : IApiResult
    {
        public List<ListingSummaryResultItem> Items { get; set; } = new List<ListingSummaryResultItem>();
    }

    public class ListingSummaryResultItem
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
