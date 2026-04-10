using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    /// <summary>
    /// Validates availability window deletion.
    /// </summary>
    public class DeleteAvailabilityWindowCommandValidator : AbstractValidator<DeleteAvailabilityWindowCommand>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public DeleteAvailabilityWindowCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}
