using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Auth;
using ParkShare.Shared.Results.Auth;

namespace ParkShare.Api.Mappings
{
    public class AuthMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<LoginRequest, LoginQuery>();
            cfg.CreateMap<RefreshTokenRequest, RefreshTokenQuery>();
            cfg.CreateMap<LoginResponse, LoginResult>();
        }
    }
}
