namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Represents an application-owned repository transaction.
    /// </summary>
    public interface IRepositoryTransaction : IAsyncDisposable
    {
        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task CommitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task RollbackAsync(CancellationToken cancellationToken);
    }
}
