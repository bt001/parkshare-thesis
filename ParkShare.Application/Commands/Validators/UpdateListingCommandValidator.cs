using FluentValidation;

namespace ParkShare.Application.Commands.Validators
{
    public class UpdateListingCommandValidator : AbstractValidator<UpdateListingCommand>
    {
        public UpdateListingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.AmenityIds).Must(ids => ids is null || (ids.All(id => id > 0) && ids.Distinct().Count() == ids.Count))
                .WithMessage("Amenity IDs must be positive and unique.");
            RuleFor(x => x.OwnerId).GreaterThan(0);
            RuleFor(x => x.Title).NotEmpty().MinimumLength(1).MaximumLength(120);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.PricePerHour).GreaterThan(0);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.Address).MaximumLength(200);
        }
    }
}
