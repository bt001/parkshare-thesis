using AutoMapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Bookings;
using ParkShare.Shared.Results.Bookings;

namespace ParkShare.Api.Mappings
{
    public class BookingMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<CreateBookingRequest, CreateBookingCommand>();
            cfg.CreateMap<CreateBookingResponse, BookingResult>();

            cfg.CreateMap<ApproveBookingRequest, ApproveBookingCommand>();
            cfg.CreateMap<ApproveBookingResponse, BookingResult>();

            cfg.CreateMap<RejectBookingRequest, RejectBookingCommand>();
            cfg.CreateMap<RejectBookingResponse, BookingResult>();

            cfg.CreateMap<GetBookingByIdRequest, GetBookingByIdQuery>();
            cfg.CreateMap<GetBookingByIdResponse, BookingResult>();

            cfg.CreateMap<GetRenterBookingsRequest, GetRenterBookingsQuery>();
            cfg.CreateMap<GetRenterBookingsResponse, GetRenterBookingsResult>();
            cfg.CreateMap<RenterBookingItem, RenterBookingItemResult>();

            cfg.CreateMap<GetOwnerBookingsRequest, GetOwnerBookingsQuery>();
            cfg.CreateMap<GetOwnerBookingsResponse, GetOwnerBookingsResult>();
            cfg.CreateMap<OwnerBookingItem, OwnerBookingItemResult>();
        }
    }

}
