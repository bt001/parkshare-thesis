using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    public class ActivateListingCommandValidator : AbstractValidator<ActivateListingCommand>
    {
        public ActivateListingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}
