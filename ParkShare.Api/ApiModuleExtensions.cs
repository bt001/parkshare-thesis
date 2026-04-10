using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ParkShare.Api
{
    public static class ApiModuleExtensions
    {
        public static IServiceCollection AddApiModule(this IServiceCollection services)
        {
            // Register filters, versioning, or API-specific dependencies here
            return services;
        }
    }
}
