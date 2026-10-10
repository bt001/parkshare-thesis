using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    /// <summary>
    /// Manages availability windows for listings.
    /// </summary>
    public interface IAvailabilityService
    {
        /// <summary>
        /// Creates an availability window for a listing.
        /// </summary>
        /// <param name="listingId">Target listing ID.</param>
        /// <param name="ownerId">Authenticated owner identity.</param>
        /// <param name="startDateTime">Window start time.</param>
        /// <param name="endDateTime">Window end time.</param>
        /// <param name="isBlocked">Whether the window blocks availability.</param>
        /// <param name="note">Optional owner note.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created window.</returns>
        Task<ServiceResult<CreateAvailabilityWindowResponse>> CreateAsync(long listingId, long ownerId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct);
        /// <summary>
        /// Updates an existing availability window.
        /// </summary>
        /// <param name="availabilityWindowId">Window ID.</param>
        /// <param name="ownerId">Authenticated owner identity.</param>
        /// <param name="startDateTime">Updated start time.</param>
        /// <param name="endDateTime">Updated end time.</param>
        /// <param name="isBlocked">Updated blocked flag.</param>
        /// <param name="note">Updated note.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The updated window.</returns>
        Task<ServiceResult<UpdateAvailabilityWindowResponse>> UpdateAsync(long availabilityWindowId, long ownerId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct);
        /// <summary>
        /// Deletes an availability window.
        /// </summary>
        /// <param name="availabilityWindowId">Window ID.</param>
        /// <param name="ownerId">Authenticated owner identity.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The deleted window ID.</returns>
        Task<ServiceResult<DeleteAvailabilityWindowResponse>> DeleteAsync(long availabilityWindowId, long ownerId, CancellationToken ct);
        /// <summary>
        /// Returns availability windows for a listing.
        /// </summary>
        /// <param name="listingId">Listing ID.</param>
        /// <param name="startDateTime">Optional lower bound.</param>
        /// <param name="endDateTime">Optional upper bound.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Matching availability windows.</returns>
        Task<ServiceResult<GetAvailabilityWindowsResponse>> GetListingAvailabilityAsync(long listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct);
        /// <summary>
        /// Returns availability windows for an owner.
        /// </summary>
        /// <param name="ownerId">Owner ID.</param>
        /// <param name="listingId">Optional listing filter.</param>
        /// <param name="startDateTime">Optional lower bound.</param>
        /// <param name="endDateTime">Optional upper bound.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Matching availability windows.</returns>
        Task<ServiceResult<GetAvailabilityWindowsResponse>> GetOwnerAvailabilityAsync(long ownerId, long? listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct);
        /// <summary>
        /// Returns the core ownership and timing data for a window.
        /// </summary>
        /// <param name="availabilityWindowId">Window ID.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Window core data.</returns>
        Task<ServiceResult<AvailabilityWindowCore>> GetWindowCoreAsync(long availabilityWindowId, CancellationToken ct);
        /// <summary>
        /// Checks whether open windows cover a requested time range.
        /// </summary>
        /// <param name="listingId">Listing ID.</param>
        /// <param name="startDateTime">Requested start time.</param>
        /// <param name="endDateTime">Requested end time.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><c>true</c> when coverage exists.</returns>
        Task<ServiceResult<bool>> HasWindowCoverageAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct);
        /// <summary>
        /// Checks whether blocked windows overlap a requested time range.
        /// </summary>
        /// <param name="listingId">Listing ID.</param>
        /// <param name="startDateTime">Requested start time.</param>
        /// <param name="endDateTime">Requested end time.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><c>true</c> when a blocked overlap exists.</returns>
        Task<ServiceResult<bool>> HasBlockedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct);
    }
}
