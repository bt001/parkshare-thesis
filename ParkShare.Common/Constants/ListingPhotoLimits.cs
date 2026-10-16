namespace ParkShare.Common.Constants;

/// <summary>Shared upload bounds for listing photos.</summary>
public static class ListingPhotoLimits
{
    /// <summary>Maximum encoded file size: five MiB.</summary>
    public const int MaxBytes = 5 * 1024 * 1024;
    /// <summary>Maximum photos attached to one listing.</summary>
    public const int MaxPhotos = 8;
}
