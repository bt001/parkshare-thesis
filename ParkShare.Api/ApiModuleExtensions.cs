using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkShare.Api.Services;
using ParkShare.Application.Interfaces;

namespace ParkShare.Api
{
    /// <summary>
    /// Registers API-layer services.
    /// </summary>
    public static class ApiModuleExtensions
    {
        /// <summary>
        /// Adds API-layer services to the dependency container.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddApiModule(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
