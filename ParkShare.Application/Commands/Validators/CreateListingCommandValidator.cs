using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    public class CreateListingCommandValidator : AbstractValidator<CreateListingCommand>
    {
        public CreateListingCommandValidator()
        {
            RuleFor(x => x.OwnerId).GreaterThan(0);

            RuleFor(x => x.Title)
                .NotEmpty()
                .MinimumLength(1)
                .MaximumLength(120);

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(2000);

            RuleFor(x => x.PricePerHour)
                .GreaterThan(0)
                .Must(HaveValidScalePrecision).WithMessage("PricePerHour must have up to 10 digits in total, with 2 decimal places.");

            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

            RuleFor(x => x.Address).MaximumLength(200);
        }

        private bool HaveValidScalePrecision(decimal value)
        {
            var scale = BitConverter.GetBytes(decimal.GetBits(value)[3])[2];
            var precision = value.ToString("G29").Replace(".", "").Replace("-", "").Length;
            return precision - scale <= 10 && scale <= 2;
        }
    }
}
