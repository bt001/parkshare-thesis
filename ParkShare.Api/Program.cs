using Microsoft.AspNetCore.Mvc.ApplicationModels;
using ParkShare.Api.Conventions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNgDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200") // <--- Angular's URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
