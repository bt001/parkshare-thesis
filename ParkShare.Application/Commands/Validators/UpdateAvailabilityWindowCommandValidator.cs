using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    /// <summary>
    /// Validates availability window updates.
    /// </summary>
    public class UpdateAvailabilityWindowCommandValidator : AbstractValidator<UpdateAvailabilityWindowCommand>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public UpdateAvailabilityWindowCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
            RuleFor(x => x.StartDateTime).LessThan(x => x.EndDateTime);
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }
}
