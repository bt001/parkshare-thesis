using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries;

/// <summary>Requests the GetListingPhotoContent operation.</summary>
public class GetListingPhotoContentQuery : IRequest<ServiceResult<ListingPhotoContentResponse>>
{
    /// <summary>Listing identity.</summary>
    public long ListingId { get; set; }
    /// <summary>Photo identity.</summary>
    public long PhotoId { get; set; }
}

/// <summary>Delegates photo rules and persistence to the application service.</summary>
public class GetListingPhotoContentHandler : IRequestHandler<GetListingPhotoContentQuery, ServiceResult<ListingPhotoContentResponse>>
{
    private readonly IListingPhotoService _service;
    /// <summary>Creates the photo handler.</summary>
    public GetListingPhotoContentHandler(IListingPhotoService service) => _service = service;
    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotoContentResponse>> Handle(GetListingPhotoContentQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetContentAsync(request.ListingId, request.PhotoId, cancellationToken);
        if (!result.IsSuccessful) return ServiceResult<ListingPhotoContentResponse>.WithErrors(result);
        return ServiceResult<ListingPhotoContentResponse>.Success(result.Payload);
    }
}
