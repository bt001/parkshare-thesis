using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands;

/// <summary>Requests the DeleteListingPhoto operation.</summary>
public class DeleteListingPhotoCommand : IRequest<ServiceResult<DeleteListingPhotoResponse>>
{
    /// <summary>Listing identity.</summary>
    public long ListingId { get; set; }
    /// <summary>Photo identity.</summary>
    public long PhotoId { get; set; }
    /// <summary>Authenticated owner identity.</summary>
    public long OwnerId { get; set; }
}

/// <summary>Delegates photo rules and persistence to the application service.</summary>
public class DeleteListingPhotoHandler : IRequestHandler<DeleteListingPhotoCommand, ServiceResult<DeleteListingPhotoResponse>>
{
    private readonly IListingPhotoService _service;
    /// <summary>Creates the photo handler.</summary>
    public DeleteListingPhotoHandler(IListingPhotoService service) => _service = service;
    /// <inheritdoc/>
    public async Task<ServiceResult<DeleteListingPhotoResponse>> Handle(DeleteListingPhotoCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(request.ListingId, request.PhotoId, request.OwnerId, cancellationToken);
        if (!result.IsSuccessful) return ServiceResult<DeleteListingPhotoResponse>.WithErrors(result);
        return ServiceResult<DeleteListingPhotoResponse>.Success(result.Payload);
    }
}
