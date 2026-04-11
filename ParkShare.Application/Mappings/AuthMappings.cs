using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Infrastructure.Automapper;

namespace ParkShare.Application.Mappings
{
    public class AuthMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<AuthenticatedUserResponse, LoginResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        }
    }
}
