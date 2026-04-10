using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>
    /// Validates owner availability queries.
    /// </summary>
    public class GetOwnerAvailabilityQueryValidator : AbstractValidator<GetOwnerAvailabilityQuery>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public GetOwnerAvailabilityQueryValidator()
        {
            RuleFor(x => x.OwnerId).GreaterThan(0);

            When(x => x.ListingId.HasValue, () =>
            {
                RuleFor(x => x.ListingId!.Value).GreaterThan(0);
            });

            When(x => x.StartDateTime.HasValue && x.EndDateTime.HasValue, () =>
            {
                RuleFor(x => x.StartDateTime!.Value).LessThan(x => x.EndDateTime!.Value);
            });
        }
    }
}
