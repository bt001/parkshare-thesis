namespace ParkShare.Application.Dto.Application
{
    public class GetOwnerListingsResponse
    {
        public List<ListingSummaryDto> Items { get; set; } = new List<ListingSummaryDto>();
    }

    public class ListingSummaryDto
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
