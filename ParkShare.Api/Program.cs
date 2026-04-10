using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParkShare.Api;
using ParkShare.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Infrastructure;
using ParkShare.Infrastructure.Configuration;
using System.Reflection;
using Autofac;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---- CONFIGURATION ----
        builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("DatabaseConfig"));

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
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}