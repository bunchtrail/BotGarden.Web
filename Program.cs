using BotGarden.Application.Services;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Applications.Services;
using BotGarden.Domain.Models;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// Настройка служб (Services Configuration)
// ======================================

// Определение политики авторизации
var useJwt = builder.Configuration.GetValue<bool>("UseJwt");

builder.Services.AddControllers(options =>
{
    if (useJwt)
    {
        var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    }
});

// Добавление описания конечных точек API для Swagger
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger с поддержкой JWT аутентификации
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BotGarden API",
        Version = "v1",
        Description = "API для управления ботаническим садом",
        Contact = new OpenApiContact
        {
            Name = "Nikita Lolenko",
            Email = "nik@loleenko.ru"
        }
    });

    if (useJwt)
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Введите 'Bearer' [пробел] и ваш токен JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "Bearer",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    }
});

// Настройка контекста базы данных с использованием PostgreSQL
builder.Services.AddDbContext<BotanicGardenContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("BotanicalDb"),
           sqlOptions => sqlOptions.UseNetTopologySuite()) // Поддержка географических данных
    .EnableSensitiveDataLogging() // Включить детальное логирование (только для разработки)
    .LogTo(Console.WriteLine, LogLevel.Information); // Логирование SQL-запросов в консоль
});

// Регистрация репозиториев
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRepository<Plants>, PlantsRepository>();
builder.Services.AddScoped<IRepository<PlantFamilies>, PlantFamiliesRepository>();
builder.Services.AddScoped<IRepository<BotGardenMode>, BotGardenRepository>();
builder.Services.AddScoped<IRepository<Genus>, GenusRepository>();

// Регистрация сервисов
builder.Services.AddScoped<PlantService>();
builder.Services.AddScoped<GenusService>();
builder.Services.AddScoped<SectorsService>();
builder.Services.AddScoped<PlantFamilyService>();
builder.Services.AddScoped<CollectionsService>();
builder.Services.AddScoped<BotGardenService>();

// Добавление авторизации
if (useJwt)
{
    builder.Services.AddAuthorization();
}
else
{
    // Политика по умолчанию, разрешающая анонимный доступ
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    });
}

// Настройка JWT аутентификации
if (useJwt)
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                ClockSkew = TimeSpan.Zero
            };
        });
}

// Включение CORS (разрешаем все запросы)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Добавление логирования
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    // Добавьте другие провайдеры логирования при необходимости
});

// ==============================
// Конфигурация конвейера (Pipeline)
// ==============================

var app = builder.Build();

// Применение миграций и инициализация данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotanicGardenContext>();
    dbContext.Database.Migrate(); // Применяем миграции
    dbContext.EnsureDefaultUser(); // Создаем пользователя по умолчанию, если его нет
}

// Настройка middleware Swagger для генерации и отображения документации
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BotGarden API V1");
    c.DocumentTitle = "BotGarden API Documentation";
});

// Включение HTTPS перенаправления
app.UseHttpsRedirection();

// Включение CORS
app.UseCors("AllowAll");

// Включение аутентификации и авторизации
if (useJwt)
{
    app.UseAuthentication();
}

// Всегда вызываем UseAuthorization
app.UseAuthorization();

// Маршрутизация контроллеров
app.MapControllers();

// Запуск приложения
app.Run();