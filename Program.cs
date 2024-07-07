using BotGarden.Application.Services;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Applications.Services;
using BotGarden.Domain.Models;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the database context
builder.Services.AddDbContext<BotanicGardenContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("BotanicalDb"),
        x => x.UseNetTopologySuite())
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

app.UseAuthorization();

app.MapControllers();

app.Run();
