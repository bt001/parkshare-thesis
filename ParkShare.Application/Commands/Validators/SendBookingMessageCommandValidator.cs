using FluentValidation;
namespace ParkShare.Application.Commands.Validators
{
    /// <summary>Validates chat input before service execution.</summary>
    public class SendBookingMessageCommandValidator : AbstractValidator<SendBookingMessageCommand>
    {
        /// <summary>Configures bounded participant input.</summary>
        public SendBookingMessageCommandValidator()
        {
            RuleFor(x => x.BookingId).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.MessageText).Must(text => !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 2000).WithMessage("Message must contain 1 to 2000 characters.");
        }
    }
}
