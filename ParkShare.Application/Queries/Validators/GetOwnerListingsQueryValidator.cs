using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    public class GetOwnerListingsQueryValidator : AbstractValidator<GetOwnerListingsQuery>
    {
        public GetOwnerListingsQueryValidator()
        {
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}
