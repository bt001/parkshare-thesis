using AutoMapper;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Domain.Entities;

namespace ParkShare.Infrastructure.Mappings
{
    public class UserMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<User, UserResponse>();
            cfg.CreateMap<User, AuthenticatedUserResponse>()
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.AccessTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.TokenType, opt => opt.Ignore());
        }
    }
}
