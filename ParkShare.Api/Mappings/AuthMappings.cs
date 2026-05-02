using AutoMapper;
using ParkShare.Application.Commands;
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
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<LoginRequest, LoginQuery>();
            cfg.CreateMap<RegisterRequest, RegisterCommand>();
            cfg.CreateMap<RefreshTokenRequest, RefreshTokenQuery>();
            cfg.CreateMap<LoginResponse, LoginResult>();
        }
    }
}
