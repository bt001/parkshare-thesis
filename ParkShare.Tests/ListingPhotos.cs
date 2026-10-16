using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Services;
using ParkShare.Common.Constants;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

internal static partial class Program
{
    private static readonly byte[] PhotoPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");

    /// <summary>Constructs the real photo workflow with an isolated filesystem.</summary>
    private static ListingPhotoService Photos(ParkShareDbContext db, IListingPhotoStorage storage, IRepositoryTransactionCoordinator? transactions = null)
        => new(new ListingRepository(db), new ListingPhotoRepository(db), storage,
            transactions ?? new RepositoryTransactionCoordinator(db), NullLogger<ListingPhotoService>.Instance);

    /// <summary>Tests supported uploads, isolation, bounds and persistence across storage/context recreation.</summary>
    private static async Task PhotoWorkflow()
    {
        var root = Path.Combine(Path.GetTempPath(), "ParkShare_Photos_" + Guid.NewGuid().ToString("N"));
        var options = Options.Create(new ListingPhotoStorageConfig { RootPath = root });
        var storage = new LocalListingPhotoStorage(options);
        try
        {
            var (listing, _, _) = await Fixture();
            var (otherListing, _, _) = await Fixture();
            long photoId;
            await using (var db = Context())
            {
                var service = Photos(db, storage);
                Check((await service.GetAsync(listing, Ct)).Payload.Items.Count == 0, "photo-free listing works");
                Check(!(await service.UploadAsync(listing, _renter, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful, "outsider upload denied");
                foreach (var (bytes, type) in new[] { (Array.Empty<byte>(), "image/png"), (PhotoPng, "image/jpeg"), (PhotoPng, "image/svg+xml"), (new byte[ListingPhotoLimits.MaxBytes + 1], "image/png"), (new byte[] { 1, 2, 3 }, "image/png") })
                    Check(!(await service.UploadAsync(listing, _owner, new MemoryStream(bytes), type, Ct)).IsSuccessful, "bad images rejected");
                var uploaded = await service.UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct);
                Check(uploaded.IsSuccessful, "PNG uploaded"); photoId = uploaded.Payload.Id;
                Check(uploaded.Payload.UploadedAt.Kind == DateTimeKind.Utc && uploaded.Payload.Url == $"api/listing/{listing}/photos/{photoId}/content", "safe metadata and UTC time");
                Check(!(await service.DeleteAsync(listing, photoId, _renter, Ct)).IsSuccessful, "outsider removal denied");
                Check(!(await service.DeleteAsync(otherListing, photoId, _owner, Ct)).IsSuccessful, "cross-listing removal denied");
                Check(!(await service.GetContentAsync(otherListing, photoId, Ct)).IsSuccessful, "cross-listing content denied");
                Check((await new ListingPhotoRepository(db).GetAsync(listing, photoId, Ct))!.Base64Content is null, "no base64 stored");
            }
            await using (var db = Context())
            {
                var service = Photos(db, new LocalListingPhotoStorage(options));
                Check((await service.GetAsync(listing, Ct)).Payload.Items.Single().Id == photoId, "metadata survives restart");
                Check((await service.GetContentAsync(listing, photoId, Ct)).Payload.Content.SequenceEqual(PhotoPng), "file survives storage recreation");
                await db.Listings.Where(l => l.Id == listing).ExecuteUpdateAsync(u => u.SetProperty(l => l.IsActive, false));
                Check((await service.UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful, "inactive listing upload");
                Check((await service.DeleteAsync(listing, photoId, _owner, Ct)).IsSuccessful, "inactive removal");
                Check(!(await service.GetContentAsync(listing, photoId, Ct)).IsSuccessful, "deleted content unavailable");
                Check(Directory.GetFiles(root).Length == 1, "file cleanup");
                using var cancel = new CancellationTokenSource(); cancel.Cancel();
                var propagated = false;
                try { await service.UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", cancel.Token); }
                catch (OperationCanceledException) { propagated = true; }
                Check(propagated, "caller cancellation propagated");
            }
            var rejected = false;
            try { await storage.ReadAsync("../escape.png", Ct); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "storage traversal rejected");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    /// <summary>Tests serialization, fresh ownership, rollback and postcommit cleanup failures.</summary>
    private static async Task PhotoTransactions()
    {
        var root = Path.Combine(Path.GetTempPath(), "ParkShare_Photos_" + Guid.NewGuid().ToString("N"));
        var storage = new LocalListingPhotoStorage(Options.Create(new ListingPhotoStorageConfig { RootPath = root }));
        try
        {
            var (listing, _, _) = await Fixture();
            await using (var db = Context(new FailPhotoInsert()))
                Check(!(await Photos(db, storage).UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful, "metadata failure safe result");
            Check(Directory.GetFiles(root).Length == 0, "confirmed rollback cleans file");
            await using (var db = Context())
            {
                Check(!await db.ListingPhotos.AnyAsync(p => p.ListingId == listing), "metadata rolled back");
                Check(!(await Photos(db, new PhotoStorageFault(storage, failWrite: true)).UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful, "partial storage failure safe result");
                Check(!await db.ListingPhotos.AnyAsync(p => p.ListingId == listing), "failed storage leaves no row");
            }
            Check(Directory.GetFiles(root).Length == 0, "partial file removed after rollback");
            await using (var db = Context())
            {
                var service = Photos(db, storage);
                var photo = (await service.UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).Payload;
                Check((await Photos(db, new PhotoStorageFault(storage, failDelete: true)).DeleteAsync(listing, photo.Id, _owner, Ct)).IsSuccessful, "cleanup failure preserves committed deletion");
                Check(!(await service.GetContentAsync(listing, photo.Id, Ct)).IsSuccessful, "orphan cannot be served");
                for (var i = 0; i < 7; i++) Check((await service.UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful, "fill photo slots");
            }
            var successes = 0;
            await Race(listing,
                async db => { if ((await Photos(db, storage).UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful) Interlocked.Increment(ref successes); },
                async db => { if ((await Photos(db, storage).UploadAsync(listing, _owner, new MemoryStream(PhotoPng), "image/png", Ct)).IsSuccessful) Interlocked.Increment(ref successes); });
            Check(successes == 1, "concurrent uploads respect final slot");
            await using (var db = Context()) Check(await db.ListingPhotos.CountAsync(p => p.ListingId == listing) == 8, "photo count bounded");

            var (freshListing, _, _) = await Fixture();
            await using (var db = Context())
            {
                var result = await Photos(db, storage, new LostPhotoCommit(new RepositoryTransactionCoordinator(db)))
                    .UploadAsync(freshListing, _owner, new MemoryStream(PhotoPng), "image/png", Ct);
                Check(!result.IsSuccessful, "lost commit acknowledgement reported as uncertain");
            }
            await using (var db = Context())
            {
                var saved = (await Photos(db, storage).GetAsync(freshListing, Ct)).Payload.Items.Single();
                Check((await Photos(db, storage).GetContentAsync(freshListing, saved.Id, Ct)).IsSuccessful, "uncertain commit retains saved file");
            }
            await using var blocker = Context();
            await using var transaction = await blocker.Database.BeginTransactionAsync();
            await new ListingRepository(blocker).LockAsync(freshListing, Ct);
            var probe = new LockSignal(); await using var waitingDb = Context(probe);
            var waiting = Photos(waitingDb, storage).UploadAsync(freshListing, _owner, new MemoryStream(PhotoPng), "image/png", Ct);
            await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Check(!waiting.IsCompleted, "photo upload waits for listing lock");
            await blocker.Listings.Where(l => l.Id == freshListing).ExecuteUpdateAsync(u => u.SetProperty(l => l.OwnerId, _renter));
            await transaction.CommitAsync();
            Check(!(await waiting).IsSuccessful, "fresh ownership checked after lock");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    /// <summary>Injects a metadata write failure after file storage.</summary>
    private sealed class FailPhotoInsert : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("ListingPhotos") && command.CommandText.Contains("INSERT")) throw new IOException("Injected metadata failure.");
            return ValueTask.FromResult(result);
        }
    }
    /// <summary>Injects partial writes and cleanup failures around real file storage.</summary>
    private sealed class PhotoStorageFault(IListingPhotoStorage inner, bool failWrite = false, bool failDelete = false) : IListingPhotoStorage
    {
        /// <inheritdoc/>
        public async Task WriteAsync(string key, byte[] content, CancellationToken ct)
        {
            await inner.WriteAsync(key, content, ct);
            if (failWrite) throw new IOException("Injected partial write failure.");
        }
        /// <inheritdoc/>
        public Task<byte[]?> ReadAsync(string key, CancellationToken ct) => inner.ReadAsync(key, ct);
        /// <inheritdoc/>
        public Task DeleteAsync(string key, CancellationToken ct) => failDelete ? throw new IOException("Injected cleanup failure.") : inner.DeleteAsync(key, ct);
    }
    /// <summary>Models a durable commit whose acknowledgement is lost.</summary>
    private sealed class LostPhotoCommit(IRepositoryTransactionCoordinator inner) : IRepositoryTransactionCoordinator
    {
        /// <inheritdoc/>
        public async Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken ct) => new LostPhotoTransaction(await inner.BeginTransactionAsync(ct));
    }
    /// <summary>Commits then raises an operational failure.</summary>
    private sealed class LostPhotoTransaction(IRepositoryTransaction inner) : IRepositoryTransaction
    {
        /// <inheritdoc/>
        public async Task CommitAsync(CancellationToken ct) { await inner.CommitAsync(ct); throw new IOException("Lost commit acknowledgement."); }
        /// <inheritdoc/>
        public Task RollbackAsync(CancellationToken ct) => inner.RollbackAsync(ct);
        /// <inheritdoc/>
        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
