using AutoMapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Queries;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Shared.Requests.Ratings;
using ParkShare.Shared.Results.Ratings;
namespace ParkShare.Api.Mappings
{
    /// <summary>Maps review API contracts.</summary>
    public class RatingMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<SaveBookingRatingRequest, SaveBookingRatingCommand>();
            cfg.CreateMap<GetListingRatingsRequest, GetListingRatingsQuery>();
            cfg.CreateMap<RatingResponse, RatingResult>();
            cfg.CreateMap<GetListingRatingsResponse, GetListingRatingsResult>();
        }
    }
}
