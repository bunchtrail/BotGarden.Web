using BotGarden.Application.Services;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Applications.Services;
using BotGarden.Domain.Models;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BotanicGardenContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("BotanicalDb"),
           sqlOptions => sqlOptions.UseNetTopologySuite())
    .LogTo(Console.WriteLine, LogLevel.Information);
});

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRepository<Plants>, PlantsRepository>();
builder.Services.AddScoped<IRepository<PlantFamilies>, PlantFamiliesRepository>();
builder.Services.AddScoped<IRepository<BotGardenMode>, BotGardenRepository>();
builder.Services.AddScoped<IRepository<Genus>, GenusRepository>();

// Register services
builder.Services.AddScoped<PlantService>();
builder.Services.AddScoped<GenusService>();
builder.Services.AddScoped<SectorsService>();
builder.Services.AddScoped<PlantFamilyService>();
builder.Services.AddScoped<CollectionsService>();
builder.Services.AddScoped<BotGardenService>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
