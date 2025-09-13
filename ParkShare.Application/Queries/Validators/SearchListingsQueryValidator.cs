using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    public class SearchListingsQueryValidator : AbstractValidator<SearchListingsQuery>
    {
        public SearchListingsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            When(x => x.RadiusKm.HasValue, () =>
            {
                RuleFor(x => x.RadiusKm!.Value).GreaterThan(0);
                RuleFor(x => x.Lat!.Value).InclusiveBetween(-90, 90);
                RuleFor(x => x.Lng!.Value).InclusiveBetween(-180, 180);
            });

            When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue, () =>
            {
                RuleFor(x => x).Must(x => x.MinPrice! <= x.MaxPrice!).WithMessage("MinPrice cannot exceed MaxPrice.");
            });

            RuleFor(x => x.TitleContains).MaximumLength(120);
            RuleFor(x => x.AddressContains).MaximumLength(200);
        }
    }
}
