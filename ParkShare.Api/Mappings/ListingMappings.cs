using AutoMapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Listings;
using ParkShare.Shared.Results.Listings;

namespace ParkShare.Api.Mappings
{
    public class ListingMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<CreateListingRequest, CreateListingCommand>()
                .ForMember(destination => destination.AmenityIds, options => options.AllowNull());
            cfg.CreateMap<CreateListingResponse, CreateListingResult>();

            cfg.CreateMap<GetListingByIdRequest, GetListingByIdQuery>();
            cfg.CreateMap<GetListingByIdResponse, GetListingByIdResult>();

            cfg.CreateMap<GetOwnerListingsRequest, GetOwnerListingsQuery>();
            cfg.CreateMap<GetOwnerListingsResponse, GetOwnerListingsResult>();
            cfg.CreateMap<ListingSummaryDto, ListingSummaryResultItem>();

            cfg.CreateMap<UpdateListingRequest, UpdateListingCommand>()
                .ForMember(destination => destination.AmenityIds, options => options.AllowNull());
            cfg.CreateMap<UpdateListingResponse, UpdateListingResult>();

            cfg.CreateMap<DeactivateListingRequest, DeactivateListingCommand>();
            cfg.CreateMap<DeactivateListingResponse, DeactivateListingResult>();

            cfg.CreateMap<ActivateListingRequest, ActivateListingCommand>();
            cfg.CreateMap<ActivateListingResponse, ActivateListingResult>();

            cfg.CreateMap<SearchListingsRequest, SearchListingsQuery>();
            cfg.CreateMap<SearchListingsResponse, SearchListingsResult>();
            cfg.CreateMap<SearchListingItemDto, SearchListingItemResult>();
        }
    }
}
