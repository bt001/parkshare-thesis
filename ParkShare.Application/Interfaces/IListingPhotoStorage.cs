namespace ParkShare.Application.Interfaces;

/// <summary>Stores bounded images under generated keys outside public files.</summary>
public interface IListingPhotoStorage
{
    /// <summary>Writes a new generated key; the caller owns rollback cleanup.</summary>
    Task WriteAsync(string key, byte[] content, CancellationToken ct);
    /// <summary>Reads bounded image bytes, or null when absent.</summary>
    Task<byte[]?> ReadAsync(string key, CancellationToken ct);
    /// <summary>Deletes a generated key, tolerating missing files.</summary>
    Task DeleteAsync(string key, CancellationToken ct);
}
