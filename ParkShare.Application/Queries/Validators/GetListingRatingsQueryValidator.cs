using FluentValidation;
namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates GetListingRatings input.</summary>
    public class GetListingRatingsQueryValidator : AbstractValidator<GetListingRatingsQuery>
    {
        /// <summary>Defines input bounds.</summary>
        public GetListingRatingsQueryValidator()
        {
            RuleFor(x => x.ListingId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
