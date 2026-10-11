using FluentValidation;
namespace ParkShare.Application.Commands.Validators
{
    /// <summary>Validates notification identity and input bounds.</summary>
    public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
    {
        /// <summary>Defines valid notification request values.</summary>
        public MarkNotificationReadCommandValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
