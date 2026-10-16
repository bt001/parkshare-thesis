using FluentValidation;
namespace ParkShare.Application.Commands.Validators;

/// <summary>Validates photo operation inputs.</summary>
public class DeleteListingPhotoCommandValidator : AbstractValidator<DeleteListingPhotoCommand>
{
    /// <summary>Configures identity and input rules.</summary>
    public DeleteListingPhotoCommandValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);
        RuleFor(x => x.PhotoId).GreaterThan(0);
        RuleFor(x => x.OwnerId).GreaterThan(0);
    }
}
