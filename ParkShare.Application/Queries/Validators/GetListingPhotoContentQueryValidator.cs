using FluentValidation;
namespace ParkShare.Application.Queries.Validators;

/// <summary>Validates photo operation inputs.</summary>
public class GetListingPhotoContentQueryValidator : AbstractValidator<GetListingPhotoContentQuery>
{
    /// <summary>Configures identity and input rules.</summary>
    public GetListingPhotoContentQueryValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);
        RuleFor(x => x.PhotoId).GreaterThan(0);
    }
}
