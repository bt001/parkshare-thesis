using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ParkShare.Api;
using ParkShare.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Infrastructure;
using ParkShare.Infrastructure.Configuration;
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
        builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("DatabaseConfig"));
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

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

        builder.Services.AddAutoMapper([typeof(AutoMapperProfile).GetTypeInfo().Assembly]);

        // ---- SWAGGER ----
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

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
