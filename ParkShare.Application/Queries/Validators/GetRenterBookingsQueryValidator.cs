using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates renter history filters and pagination.</summary>
    public class GetRenterBookingsQueryValidator : AbstractValidator<GetRenterBookingsQuery>
    {
        /// <summary>Defines valid identity, paging, and status values.</summary>
        public GetRenterBookingsQueryValidator()
        {
            RuleFor(x => x.RenterId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            When(x => x.Statuses != null, () => RuleForEach(x => x.Statuses!).IsInEnum());
        }
    }
}
