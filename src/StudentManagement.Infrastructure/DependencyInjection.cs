using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StudentManagement.Application.Interfaces;
using StudentManagement.Domain.Interfaces;
using StudentManagement.Infrastructure.Data;
using StudentManagement.Infrastructure.Identity;
using StudentManagement.Infrastructure.Repositories;
using StudentManagement.Infrastructure.Services;

namespace StudentManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. DbContext
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string is missing.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // 2. Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Enterprise Password Policy
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // Enterprise Lockout Policy
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // 3. JWT Authentication
            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing.");
            var jwtIssuer = configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
            var jwtAudience = configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("Jwt:Audience is missing.");

            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Key must be at least 32 characters (256 bits).");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set true in strict production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

            // 4. Repositories and Unit of Work
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IStudentRepository, StudentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 5. Application Services Implementations
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
