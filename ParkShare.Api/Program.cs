using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ParkShare.Api;
using ParkShare.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Infrastructure;
using ParkShare.Infrastructure.Configuration;
using ParkShare.Infrastructure.Persistence;
using System.Reflection;
using Autofac;
using System;
using System.Text;
using System.Threading.Tasks;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---- CONFIGURATION ----
        builder.Services.Configure<PlaceSearchConfig>(builder.Configuration.GetSection("PlaceSearch"));
        builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("DatabaseConfig"));
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

        builder.Services.Configure<ListingPhotoStorageConfig>(builder.Configuration.GetSection("ListingPhotoStorage"));
        builder.Services.PostConfigure<ListingPhotoStorageConfig>(options =>
            options.RootPath = System.IO.Path.GetFullPath(options.RootPath, builder.Environment.ContentRootPath));

        // ---- MODULES ----

        builder.Services.AddApiModule();
        builder.Services.AddApplicationModule();
        builder.Services.AddInfrastructureModule();

        // ---- CONTROLLERS & ROUTING ----
        builder.Services.AddControllers();

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true; // Ensures all URLs are lowercase
            options.LowercaseQueryStrings = true; // Ensures query strings are lowercase
        });

        // ---- AUTHENTICATION ----
        var jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfig>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenType = context.Principal?.FindFirst("token_type")?.Value;
                        if (!string.Equals(tokenType, "access", StringComparison.Ordinal))
                            context.Fail("Invalid access token type.");

                        return Task.CompletedTask;
                    }
                };
            });

        // ---- CORS ----
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowNgDev", policy =>
            {
                policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // ---- AUTOMAPPER ----
        // AutoMapper remains in use here for thesis delivery speed and readability.
        // This repository is a non-commercial academic project, so a local
        // Community license key can be supplied via configuration without
        // committing secrets to source control. If ParkShare evolves into a
        // real production product, revisit both the mapping approach and the
        // long-term licensing/compliance decision.
        var autoMapperLicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
        builder.Services.AddAutoMapper(
            cfg =>
            {
                if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
                    cfg.LicenseKey = autoMapperLicenseKey;
            },
            [typeof(AutoMapperProfile).GetTypeInfo().Assembly]);

        // ---- SWAGGER ----
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ParkShareDbContext>();
            dbContext.Database.Migrate();
        }

        // ---- MIDDLEWARE PIPELINE ----
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowNgDev");

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
