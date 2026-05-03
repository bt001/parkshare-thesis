namespace ParkShare.Application.Dto.Infrastructure
{
    /// <summary>
    /// Represents a paged repository query result.
    /// </summary>
    /// <typeparam name="T">Returned item type.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Creates a paged result.
        /// </summary>
        /// <param name="items">Returned items.</param>
        /// <param name="totalCount">Total matching item count.</param>
        public PagedResult(IReadOnlyList<T> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }

        /// <summary>
        /// Returned page items.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Total matching item count.
        /// </summary>
        public int TotalCount { get; }
    }
}
