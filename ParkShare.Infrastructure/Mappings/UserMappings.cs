using AutoMapper;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;

namespace ParkShare.Infrastructure.Mappings
{
    /// <summary>
    /// Registers user mappings for infrastructure DTOs.
    /// </summary>
    public class UserMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .ToCanonicalRoleString()))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles
                    .OrderBy(userRole => userRole.RoleId)
                    .Select(userRole => userRole.Role.Name)
                    .ToArray()));

            cfg.CreateMap<User, AuthenticatedUserResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.UserRoles
                    .Select(userRole => userRole.Role.Name)
                    .ToCanonicalRoleString()))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles
                    .OrderBy(userRole => userRole.RoleId)
                    .Select(userRole => userRole.Role.Name)
                    .ToArray()))
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.AccessTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.TokenType, opt => opt.Ignore());
        }
    }
}
