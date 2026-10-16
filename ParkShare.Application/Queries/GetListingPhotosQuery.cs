using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries;

/// <summary>Requests the GetListingPhotos operation.</summary>
public class GetListingPhotosQuery : IRequest<ServiceResult<ListingPhotosResponse>>
{
    /// <summary>Listing identity.</summary>
    public long ListingId { get; set; }
}

/// <summary>Delegates photo rules and persistence to the application service.</summary>
public class GetListingPhotosHandler : IRequestHandler<GetListingPhotosQuery, ServiceResult<ListingPhotosResponse>>
{
    private readonly IListingPhotoService _service;
    /// <summary>Creates the photo handler.</summary>
    public GetListingPhotosHandler(IListingPhotoService service) => _service = service;
    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotosResponse>> Handle(GetListingPhotosQuery request, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(request.ListingId, cancellationToken);
        if (!result.IsSuccessful) return ServiceResult<ListingPhotosResponse>.WithErrors(result);
        return ServiceResult<ListingPhotosResponse>.Success(result.Payload);
    }
}
