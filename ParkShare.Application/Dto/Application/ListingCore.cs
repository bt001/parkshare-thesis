namespace ParkShare.Application.Dto.Application
{
    public class ListingCore
    {
        public long Id { get; set; }
        public long OwnerId { get; set; }
        public bool IsActive { get; set; }
        public bool IsAlwaysAvailable { get; set; }
    }
}
