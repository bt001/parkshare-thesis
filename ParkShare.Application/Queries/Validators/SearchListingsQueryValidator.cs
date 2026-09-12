using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates listing search filters and paging.</summary>
    public class SearchListingsQueryValidator : AbstractValidator<SearchListingsQuery>
    {
        /// <summary>Defines valid search ranges and required radius coordinates.</summary>
        public SearchListingsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            When(x => x.RadiusKm.HasValue, () =>
            {
                RuleFor(x => x.RadiusKm!.Value).GreaterThan(0);
                RuleFor(x => x.Lat).NotNull().InclusiveBetween(-90, 90);
                RuleFor(x => x.Lng).NotNull().InclusiveBetween(-180, 180);
            });

            RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0);

            When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue, () =>
            {
                RuleFor(x => x).Must(x => x.MinPrice! <= x.MaxPrice!).WithMessage("MinPrice cannot exceed MaxPrice.");
            });

            RuleFor(x => x.TitleContains).MaximumLength(120);
            RuleFor(x => x.AddressContains).MaximumLength(200);
        }
    }
}
