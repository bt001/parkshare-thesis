namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Starts repository transactions for application services.
    /// </summary>
    public interface IRepositoryTransactionCoordinator
    {
        /// <summary>
        /// Begins a new transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Started transaction.</returns>
        Task<IRepositoryTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    }
}
