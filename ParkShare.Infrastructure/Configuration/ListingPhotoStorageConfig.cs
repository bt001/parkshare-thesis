namespace ParkShare.Infrastructure.Configuration;

/// <summary>Private local photo directory; relative paths resolve from the API content root.</summary>
public class ListingPhotoStorageConfig
{
    /// <summary>Directory retained across API restarts; do not place it under wwwroot.</summary>
    public string RootPath { get; set; } = "App_Data/listing-photos";
}
