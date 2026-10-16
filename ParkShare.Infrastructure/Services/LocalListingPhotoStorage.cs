using Microsoft.Extensions.Options;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Constants;
using ParkShare.Infrastructure.Configuration;
namespace ParkShare.Infrastructure.Services;

/// <inheritdoc/>
public class LocalListingPhotoStorage : IListingPhotoStorage
{
    private readonly string _root;
    /// <summary>Creates private local storage with a stable configured directory.</summary>
    public LocalListingPhotoStorage(IOptions<ListingPhotoStorageConfig> options) => _root = Path.GetFullPath(options.Value.RootPath);
    /// <summary>Accepts only generated image keys, never client paths.</summary>
    private string Resolve(string key)
    {
        var extension = Path.GetExtension(key);
        if (extension is not (".jpg" or ".png") || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(key), "N", out _)
            || Path.GetFileName(key) != key || key.Contains('/') || key.Contains('\\'))
            throw new InvalidDataException("Invalid photo storage key.");
        return Path.Combine(_root, key);
    }
    /// <inheritdoc/>
    public async Task WriteAsync(string key, byte[] content, CancellationToken ct)
    {
        if (content.Length is 0 or > ListingPhotoLimits.MaxBytes) throw new InvalidDataException("Invalid photo size.");
        var path = Resolve(key);
        Directory.CreateDirectory(_root);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.WriteAsync(content, ct);
    }
    /// <inheritdoc/>
    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct)
    {
        var path = Resolve(key);
        try
        {
            await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, true);
            if (file.Length is 0 or > ListingPhotoLimits.MaxBytes) throw new InvalidDataException("Invalid stored photo size.");
            var content = new byte[(int)file.Length];
            await file.ReadExactlyAsync(content, ct);
            return content;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
    /// <inheritdoc/>
    public Task DeleteAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        File.Delete(Resolve(key));
        return Task.CompletedTask;
    }
}
