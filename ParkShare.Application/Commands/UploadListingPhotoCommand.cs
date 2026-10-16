using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands;

/// <summary>Requests the UploadListingPhoto operation.</summary>
public class UploadListingPhotoCommand : IRequest<ServiceResult<ListingPhotoResponse>>
{
    /// <summary>Listing identity.</summary>
    public long ListingId { get; set; }
    /// <summary>Authenticated owner identity.</summary>
    public long OwnerId { get; set; }
    /// <summary>Request-owned bounded image stream.</summary>
    public Stream Content { get; set; } = Stream.Null;
    /// <summary>Declared image media type.</summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>Delegates photo rules and persistence to the application service.</summary>
public class UploadListingPhotoHandler : IRequestHandler<UploadListingPhotoCommand, ServiceResult<ListingPhotoResponse>>
{
    private readonly IListingPhotoService _service;
    /// <summary>Creates the photo handler.</summary>
    public UploadListingPhotoHandler(IListingPhotoService service) => _service = service;
    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotoResponse>> Handle(UploadListingPhotoCommand request, CancellationToken cancellationToken)
    {
        var result = await _service.UploadAsync(request.ListingId, request.OwnerId, request.Content, request.ContentType, cancellationToken);
        if (!result.IsSuccessful) return ServiceResult<ListingPhotoResponse>.WithErrors(result);
        return ServiceResult<ListingPhotoResponse>.Success(result.Payload);
    }
}
