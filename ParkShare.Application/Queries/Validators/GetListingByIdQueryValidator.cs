using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    public class GetListingByIdQueryValidator : AbstractValidator<GetListingByIdQuery>
    {
        public GetListingByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
