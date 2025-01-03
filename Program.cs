﻿// src/BotGarden.Web/Program.cs
using BotGarden.Application.Services;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Applications.Services;
using BotGarden.Domain.Models;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Infrastructure.Data.Repositories;
using BotGarden.Web.Filters; // Added for FileUploadOperationFilter
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders; // Added for PhysicalFileProvider
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// Configuration Settings
// ======================================

// Read UseJwt setting from configuration
var useJwt = builder.Configuration.GetValue<bool>("UseJwt");

// Setup Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug(); // Detailed logging
});

// Setup Controllers with Authorization
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

// Add HealthChecks
builder.Services.AddHealthChecks();

// Setup Swagger
builder.Services.AddEndpointsApiExplorer();
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

    // Register Operation Filter for file uploads
    c.OperationFilter<FileUploadOperationFilter>();

    // Enable XML comments (optional)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Setup DbContext with PostgreSQL and NetTopologySuite
builder.Services.AddDbContext<BotanicGardenContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("BotanicalDb"),
           sqlOptions => sqlOptions.UseNetTopologySuite())
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information);
});

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRepository<Plants>, PlantsRepository>();
builder.Services.AddScoped<IRepository<PlantFamilies>, PlantFamiliesRepository>();
builder.Services.AddScoped<IRepository<BotGardenMode>, BotGardenRepository>();
builder.Services.AddScoped<IRepository<Genus>, GenusRepository>();

// Register Services
builder.Services.AddScoped<PlantService>();
builder.Services.AddScoped<GenusService>();
builder.Services.AddScoped<SectorsService>();
builder.Services.AddScoped<PlantFamilyService>();
builder.Services.AddScoped<CollectionsService>();
builder.Services.AddScoped<BotGardenService>();

// Setup Authentication and Authorization
if (useJwt)
{
    builder.Services.AddAuthorization();

    // Configure JWT Authentication
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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                ClockSkew = TimeSpan.Zero
            };

            // Support sending token via Authorization header
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Headers["Authorization"]
                        .FirstOrDefault()
                        ?.Split(" ")
                        .Last();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });
}
else
{
    // Default policy allowing anonymous access
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
    });
}

// Setup CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policyBuilder =>
    {
        policyBuilder
           .WithOrigins(
               "http://localhost:5173",
               "http://localhost:80",
               "http://localhost:8080",
               "http://localhost"
           )
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials();
    });

    // Доп. политика для статических файлов (если вдруг понадобится отдельно)
    options.AddPolicy("AllowStaticFiles", policyBuilder =>
    {
        policyBuilder
           .WithOrigins(
               "http://localhost:5173",
               "http://localhost:80",
               "http://localhost:8080",
               "http://localhost"
           )
           .AllowAnyMethod()
           .AllowAnyHeader()
           .WithExposedHeaders("Content-Disposition")
           .SetIsOriginAllowed(_ => true);
    });
});

// Build the application
var app = builder.Build();

// ======================================
// Middleware Configuration
// ======================================

// Enable detailed error pages in development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map HealthChecks endpoint
app.MapHealthChecks("/health");

// Готовим папку Uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Настраиваем отдачу статических файлов с CORS заголовками
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Methods", "GET");
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Headers", "Content-Type");
        ctx.Context.Response.Headers.Append(
            "Cross-Origin-Resource-Policy", "cross-origin");
    }
});

// Apply migrations and initialize data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<BotanicGardenContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying migrations or initializing data.");
        throw;
    }
}

// Configure Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BotGarden API V1");
    c.DocumentTitle = "BotGarden API Documentation";
});

// Enable CORS
app.UseCors("AllowSpecificOrigin");

// Authentication / Authorization
if (useJwt)
{
    app.UseAuthentication();
}
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// Run the application
app.Run();
