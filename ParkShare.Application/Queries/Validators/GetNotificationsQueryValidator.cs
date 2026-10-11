using FluentValidation;
namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates notification identity and input bounds.</summary>
    public class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
    {
        /// <summary>Defines valid notification request values.</summary>
        public GetNotificationsQueryValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
