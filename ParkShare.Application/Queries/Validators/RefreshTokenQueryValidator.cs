using FluentValidation;

namespace ParkShare.Application.Queries.Validators
{
    /// <summary>
    /// Validates token refresh queries.
    /// </summary>
    public class RefreshTokenQueryValidator : AbstractValidator<RefreshTokenQuery>
    {
        /// <summary>
        /// Creates the validator.
        /// </summary>
        public RefreshTokenQueryValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
