using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Services;

namespace ParkShare.Infrastructure
{
    public static class InfrastructureModuleExtensions
    {            
        public static IServiceCollection AddInfrastructureModule(this IServiceCollection services)
        {
            services.AddDbContext<ParkShareDbContext>((serviceProvider, options) =>
            {
                var dbConfig = serviceProvider
                    .GetRequiredService<IOptionsSnapshot<DatabaseConfig>>()
                    .Value.ParkShareDb;

                var connectionString = dbConfig.ConnectionString
                    //.Replace("{User}", dbConfig.User)
                    //.Replace("{Password}", dbConfig.Password)
                    .Replace("{ServerUrl}", dbConfig.ServerUrl)
                    .Replace("{InitialDb}", dbConfig.InitialDb);

                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IListingService, ListingService>();

            return services;
        }
    }
}
