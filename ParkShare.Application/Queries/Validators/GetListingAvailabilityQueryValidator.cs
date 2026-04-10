using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>
    /// Validates listing availability queries.
    /// </summary>
    public class GetListingAvailabilityQueryValidator : AbstractValidator<GetListingAvailabilityQuery>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public GetListingAvailabilityQueryValidator()
        {
            RuleFor(x => x.ListingId).GreaterThan(0);

            When(x => x.StartDateTime.HasValue && x.EndDateTime.HasValue, () =>
            {
                RuleFor(x => x.StartDateTime!.Value).LessThan(x => x.EndDateTime!.Value);
            });
        }
    }
}
