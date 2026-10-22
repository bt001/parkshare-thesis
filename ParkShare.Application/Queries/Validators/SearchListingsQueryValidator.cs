using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates listing search filters and paging.</summary>
    public class SearchListingsQueryValidator : AbstractValidator<SearchListingsQuery>
    {
        /// <summary>Defines valid search ranges and required radius coordinates.</summary>
        public SearchListingsQueryValidator()
        {
            RuleFor(x => x).Must(x => {
                var bounds = new[] { x.South, x.North, x.West, x.East };
                return bounds.All(v => !v.HasValue) || (bounds.All(v => v.HasValue && double.IsFinite(v.Value))
                    && x.South >= -90 && x.North <= 90 && x.South < x.North
                    && x.West >= -180 && x.West <= 180 && x.East >= -180 && x.East <= 180 && x.West != x.East
                    && !(x.West == 180 && x.East == -180)
                    && !x.Lat.HasValue && !x.Lng.HasValue && !x.RadiusKm.HasValue);
            }).WithMessage("Provide complete valid bounds without a radius search; south must be below north and west must differ from east.");
            RuleForEach(x => x.AmenityIds).GreaterThan(0);
            RuleFor(x => x.MinRating).InclusiveBetween(1, 5);
            RuleFor(x => x.SortBy).Must(value => value is null or "newest" or "priceAsc" or "priceDesc" or "ratingDesc")
                .WithMessage("Unsupported search ordering.");
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x).Must(x => x.Lat.HasValue == x.Lng.HasValue && x.Lat.HasValue == x.RadiusKm.HasValue)
                .WithMessage("Provide latitude, longitude and radius together.");
            RuleFor(x => x).Must(x => x.StartDateTime.HasValue == x.EndDateTime.HasValue)
                .WithMessage("Provide both arrival and departure.");
            When(x => x.StartDateTime.HasValue && x.EndDateTime.HasValue, () =>
                RuleFor(x => x).Custom((x, context) =>
                {
                    var error = new ParkShare.Application.Services.BookingTimePolicy(TimeProvider.System)
                        .Validate(x.StartDateTime!.Value, x.EndDateTime!.Value);
                    if (error != null) context.AddFailure(error);
                }));
            When(x => x.RadiusKm.HasValue, () =>
            {
                RuleFor(x => x.RadiusKm!.Value).InclusiveBetween(0.1, 100);
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
