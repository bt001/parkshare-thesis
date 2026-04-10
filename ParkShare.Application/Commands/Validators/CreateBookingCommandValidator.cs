using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
    {
        public CreateBookingCommandValidator()
        {
            RuleFor(x => x.ListingId).GreaterThan(0);
            RuleFor(x => x.RenterId).GreaterThan(0);
            RuleFor(x => x.StartDateTime).LessThan(x => x.EndDateTime);
        }
    }
}
