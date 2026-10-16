namespace ParkShare.Shared.Results.Listings;

/// <summary>Public photo metadata without filesystem details.</summary>
public class ListingPhotoResult : IApiResult
{
    /// <summary>Photo identity.</summary>
    public long Id { get; set; }
    /// <summary>API-relative content endpoint.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>UTC upload timestamp.</summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>Ordered listing photo metadata.</summary>
public class ListingPhotosResult : IApiResult
{
    /// <summary>Photos in stable upload order.</summary>
    public IReadOnlyList<ListingPhotoResult> Items { get; set; } = [];
}

/// <summary>Removed photo identity.</summary>
public class DeleteListingPhotoResult : IApiResult
{
    /// <summary>Removed identity.</summary>
    public long Id { get; set; }
}
