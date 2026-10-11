using FluentValidation;
namespace ParkShare.Application.Queries.Validators
{
    /// <summary>Validates notification identity and input bounds.</summary>
    public class GetUnreadNotificationCountQueryValidator : AbstractValidator<GetUnreadNotificationCountQuery>
    {
        /// <summary>Defines valid notification request values.</summary>
        public GetUnreadNotificationCountQueryValidator()
        {
            RuleFor(x => x.UserId).GreaterThan(0);
        }
    }
}
