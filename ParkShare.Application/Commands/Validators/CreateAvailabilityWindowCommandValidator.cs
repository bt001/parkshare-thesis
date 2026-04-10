using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    /// <summary>
    /// Validates availability window creation.
    /// </summary>
    public class CreateAvailabilityWindowCommandValidator : AbstractValidator<CreateAvailabilityWindowCommand>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public CreateAvailabilityWindowCommandValidator()
        {
            RuleFor(x => x.ListingId).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
            RuleFor(x => x.StartDateTime).LessThan(x => x.EndDateTime);
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }
}
