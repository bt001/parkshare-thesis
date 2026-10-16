using FluentValidation;
namespace ParkShare.Application.Queries.Validators;

/// <summary>Validates photo operation inputs.</summary>
public class GetListingPhotosQueryValidator : AbstractValidator<GetListingPhotosQuery>
{
    /// <summary>Configures identity and input rules.</summary>
    public GetListingPhotosQueryValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);
    }
}
