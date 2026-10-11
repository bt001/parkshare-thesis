using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

namespace ParkShare.Infrastructure
{
    /// <summary>
    /// Registers infrastructure services and repositories.
    /// </summary>
    public static class InfrastructureModuleExtensions
    {
        /// <summary>
        /// Adds the infrastructure module.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Updated service collection.</returns>
        public static IServiceCollection AddInfrastructureModule(this IServiceCollection services)
        {
            services.AddDbContext<ParkShareDbContext>((serviceProvider, options) =>
            {
                var dbConfig = serviceProvider
                    .GetRequiredService<IOptionsSnapshot<DatabaseConfig>>()
                    .Value.ParkShareDb;

                var connectionString = dbConfig.ConnectionString
                    .Replace("{ServerUrl}", dbConfig.ServerUrl)
                    .Replace("{InitialDb}", dbConfig.InitialDb);

                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IListingRepository, ListingRepository>();
            services.AddScoped<IAvailabilityWindowRepository, AvailabilityWindowRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IRepositoryTransactionCoordinator, RepositoryTransactionCoordinator>();

            return services;
        }
    }
}
