using FluentValidation;
namespace ParkShare.Application.Commands.Validators
{
    /// <summary>Validates CompleteBooking input.</summary>
    public class CompleteBookingCommandValidator : AbstractValidator<CompleteBookingCommand>
    {
        /// <summary>Defines input bounds.</summary>
        public CompleteBookingCommandValidator()
        {
            RuleFor(x => x.BookingId).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}
