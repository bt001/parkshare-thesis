namespace ParkShare.Application.Services
{
    /// <summary>Validates Romanian booking wall-clock values without relying on the server time zone.</summary>
    public sealed class BookingTimePolicy
    {
        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");
        private readonly TimeProvider _clock;

        /// <summary>Creates the time policy with an injectable UTC clock.</summary>
        public BookingTimePolicy(TimeProvider clock) => _clock = clock;

        /// <summary>Returns whether a valid Romanian booking end has been reached by the server clock.</summary>
        public bool HasEnded(DateTime end)
            => end.Kind == DateTimeKind.Unspecified && !Zone.IsInvalidTime(end) && !Zone.IsAmbiguousTime(end)
                && TimeZoneInfo.ConvertTimeToUtc(end, Zone) <= _clock.GetUtcNow().UtcDateTime;

        /// <summary>Returns a validation message or null for a future, unambiguous booking period.</summary>
        public string? Validate(DateTime start, DateTime end)
        {
            if (start.Kind != DateTimeKind.Unspecified || end.Kind != DateTimeKind.Unspecified)
                return "Use Romanian local times without a UTC suffix or offset.";
            if (start >= end)
                return "End time must be after start time.";
            if (Zone.IsInvalidTime(start) || Zone.IsInvalidTime(end)
                || Zone.IsAmbiguousTime(start) || Zone.IsAmbiguousTime(end))
                return "Choose times outside the daylight-saving clock change in Romania.";
            if (TimeZoneInfo.ConvertTimeToUtc(start, Zone) <= _clock.GetUtcNow().UtcDateTime)
                return "Booking start time must be in the future (Romanian time).";
            return null;
        }
    }
}
