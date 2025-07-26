using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Users;

namespace ParkShare.Api.Mappings
{
    public class UserMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<GetUserByEmailRequest, GetUserByEmailQuery>();
            cfg.CreateMap<GetUserByEmailResponse, GetUserByEmailResult>();
        }
    }
}
