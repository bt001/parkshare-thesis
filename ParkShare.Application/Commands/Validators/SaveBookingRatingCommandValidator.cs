using FluentValidation;
namespace ParkShare.Application.Commands.Validators
{
    /// <summary>Validates SaveBookingRating input.</summary>
    public class SaveBookingRatingCommandValidator : AbstractValidator<SaveBookingRatingCommand>
    {
        /// <summary>Defines input bounds.</summary>
        public SaveBookingRatingCommandValidator()
        {
            RuleFor(x => x.BookingId).GreaterThan(0);
            RuleFor(x => x.RenterId).GreaterThan(0);
            RuleFor(x => x.Stars).InclusiveBetween(1, 5);
            RuleFor(x => x.Comment).Must(x => (x?.Trim().Length ?? 0) <= 1000).WithMessage("Comment must contain at most 1000 characters.");
        }
    }
}
