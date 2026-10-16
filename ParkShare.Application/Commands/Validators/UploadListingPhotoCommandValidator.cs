using FluentValidation;
namespace ParkShare.Application.Commands.Validators;

/// <summary>Validates photo operation inputs.</summary>
public class UploadListingPhotoCommandValidator : AbstractValidator<UploadListingPhotoCommand>
{
    /// <summary>Configures identity and input rules.</summary>
    public UploadListingPhotoCommandValidator()
    {
        RuleFor(x => x.ListingId).GreaterThan(0);
        RuleFor(x => x.OwnerId).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.ContentType).Must(type => type is "image/jpeg" or "image/png").WithMessage("Select a JPEG or PNG image.");
    }
}
