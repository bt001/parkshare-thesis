namespace ParkShare.Application.Dto.Application;

/// <summary>Public metadata for a saved listing photo.</summary>
public class ListingPhotoResponse
{
    /// <summary>Photo identity.</summary>
    public long Id { get; set; }
    /// <summary>API-relative image endpoint.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>UTC upload timestamp.</summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>Ordered photos for a listing.</summary>
public class ListingPhotosResponse
{
    /// <summary>Saved photos in upload order.</summary>
    public IReadOnlyList<ListingPhotoResponse> Items { get; set; } = [];
}

/// <summary>Bounded image content read from private storage.</summary>
public class ListingPhotoContentResponse
{
    /// <summary>Image bytes.</summary>
    public byte[] Content { get; set; } = [];
    /// <summary>Validated image media type.</summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>Result of removing a photo.</summary>
public class DeleteListingPhotoResponse
{
    /// <summary>Removed photo identity.</summary>
    public long Id { get; set; }
}
