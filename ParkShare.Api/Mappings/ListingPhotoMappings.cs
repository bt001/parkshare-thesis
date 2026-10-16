using AutoMapper;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Shared.Results.Listings;
namespace ParkShare.Api.Mappings;

/// <summary>Maps photo metadata to public contracts.</summary>
public class ListingPhotoMappings : IHaveCustomMapping
{
    /// <inheritdoc/>
    public void CreateMappings(Profile cfg)
    {
        cfg.CreateMap<ListingPhotoResponse, ListingPhotoResult>();
        cfg.CreateMap<ListingPhotosResponse, ListingPhotosResult>();
        cfg.CreateMap<DeleteListingPhotoResponse, DeleteListingPhotoResult>();
    }
}
