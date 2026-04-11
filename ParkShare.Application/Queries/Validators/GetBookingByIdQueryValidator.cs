using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>
    /// Validates booking detail queries.
    /// </summary>
    public class GetBookingByIdQueryValidator : AbstractValidator<GetBookingByIdQuery>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public GetBookingByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.RequesterId).GreaterThan(0);
        }
    }
}
