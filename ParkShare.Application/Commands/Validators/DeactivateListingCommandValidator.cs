using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    public class DeactivateListingCommandValidator : AbstractValidator<DeactivateListingCommand>
    {
        public DeactivateListingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}
