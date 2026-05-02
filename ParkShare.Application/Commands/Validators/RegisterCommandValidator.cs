using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    /// <summary>
    /// Validates registration requests.
    /// </summary>
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public RegisterCommandValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100);

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
