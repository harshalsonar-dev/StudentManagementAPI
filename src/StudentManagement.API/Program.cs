using Microsoft.OpenApi.Models;
using Serilog;
using StudentManagement.API.Extensions;
using StudentManagement.API.Middleware;
using StudentManagement.Application;
using StudentManagement.Infrastructure;
using StudentManagement.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 1. Setup Structured Logging with Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}")
    .WriteTo.File("logs/student-api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting StudentManagement Enterprise API...");

    // 🔹 2. Clean Architecture Layer Registrations
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // 🔹 3. Enterprise Rate Limiting
    builder.Services.AddEnterpriseRateLimiting();

    // 🔹 4. Secure CORS Configuration
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("EnterpriseCorsPolicy", policy =>
        {
            if (allowedOrigins.Length != 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        });
    });

    // 🔹 5. Controllers & Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "StudentManagement Enterprise API",
            Version = "v1",
            Description = "Clean Architecture, CQRS (MediatR), JWT + Refresh Token, RBAC, Rate Limiting, and Serilog.",
            Contact = new OpenApiContact
            {
                Name = "Enterprise Architecture Team",
                Email = "architecture@enterprise.edu"
            }
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token in the format: Bearer {token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // 🔹 6. Database Migration & Enterprise Seeding
    await DbSeeder.SeedAsync(app.Services);

    // 🔹 7. Middleware Pipeline
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "StudentManagement Enterprise API v1");
            c.RoutePrefix = string.Empty; // Open Swagger at root (http://localhost:port/)
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("EnterpriseCorsPolicy");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
