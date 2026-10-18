namespace ParkShare.Application.Dto.Infrastructure;

/// <summary>Projected listing search summary without loaded navigation collections.</summary>
public class ListingSearchRow
{
    /// <summary>Listing identifier.</summary>
    public long Id { get; set; }
    /// <summary>Listing title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Optional street address.</summary>
    public string? Address { get; set; }
    /// <summary>Persisted WGS84 latitude.</summary>
    public decimal Latitude { get; set; }
    /// <summary>Persisted WGS84 longitude.</summary>
    public decimal Longitude { get; set; }
    /// <summary>Hourly price in RON.</summary>
    public decimal PricePerHour { get; set; }
    /// <summary>Whether the listing is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Whether open windows are unnecessary.</summary>
    public bool IsAlwaysAvailable { get; set; }
    /// <summary>Listing creation timestamp used for stable ordering.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Earliest uploaded photo identifier, or null.</summary>
    public long? CoverPhotoId { get; set; }
    /// <summary>Unrounded review average, or null without reviews.</summary>
    public double? AverageRating { get; set; }
    /// <summary>Number of saved reviews.</summary>
    public int RatingCount { get; set; }
}
