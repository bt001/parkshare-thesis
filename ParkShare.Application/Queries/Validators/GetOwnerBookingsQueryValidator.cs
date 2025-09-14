using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    public class GetOwnerBookingsQueryValidator : AbstractValidator<GetOwnerBookingsQuery>
    {
        public GetOwnerBookingsQueryValidator()
        {
            RuleFor(x => x.OwnerId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            When(x => x.ListingId.HasValue, () =>
            {
                RuleFor(x => x.ListingId!.Value).GreaterThan(0);
            });

            When(x => x.Statuses != null, () =>
            {
                RuleForEach(x => x.Statuses!).IsInEnum();
            });
        }
    }
}
