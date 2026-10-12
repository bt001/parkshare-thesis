using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ParkShare.Application.Infrastructure;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Services;

namespace ParkShare.Application
{
    /// <summary>
    /// Registers the application module.
    /// </summary>
    public static class ApplicationModuleExtensions
    {
        /// <summary>
        /// Adds application services, handlers, and validators.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Updated service collection.</returns>
        public static IServiceCollection AddApplicationModule(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationModuleExtensions).Assembly));
            services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IListingService, ListingService>();
            services.AddScoped<IAmenityService, AmenityService>();
            services.AddScoped<IListingReadService, ListingReadService>();
            services.AddScoped<IAvailabilityService, AvailabilityService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IRatingService, RatingService>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<BookingTimePolicy>();

            return services;
        }
    }
}
