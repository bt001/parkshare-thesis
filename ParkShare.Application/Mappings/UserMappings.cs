using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Infrastructure.Automapper;

namespace ParkShare.Application.Mappings
{
    public class UserMappings : IHaveCustomMapping
    {
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<UserResponse, GetUserByEmailResponse>();
        }
    }
}
