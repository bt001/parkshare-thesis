using FluentValidation;
namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates chat input before service execution.</summary>
    public class GetBookingMessagesQueryValidator : AbstractValidator<GetBookingMessagesQuery>
    {
        /// <summary>Configures bounded participant input.</summary>
        public GetBookingMessagesQueryValidator()
        {
            RuleFor(x => x.BookingId).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
