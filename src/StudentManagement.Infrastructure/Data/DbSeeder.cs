using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Enums;
using StudentManagement.Infrastructure.Identity;

namespace StudentManagement.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var config = scope.ServiceProvider.GetService<IConfiguration>();
            var env = scope.ServiceProvider.GetService<IHostEnvironment>();

            try
            {
                // In Development, ensure database schema is created
                if (env == null || env.IsDevelopment())
                {
                    await context.Database.MigrateAsync();
                }

                // 1. Seed Roles
                foreach (var role in AppRoles.All)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Enterprise Role '{Role}' created.", role);
                    }
                }

                // 2. Seed Initial Admin if securely configured
                var adminEmail = config?["InitialAdmin:Email"]?.Trim();
                var adminPassword = config?["InitialAdmin:Password"]?.Trim();

                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                {
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        adminUser = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FullName = "System Administrator",
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(adminUser, adminPassword);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                            logger.LogInformation("Initial administrator user created successfully from configuration.");
                        }
                        else
                        {
                            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                            logger.LogWarning("Failed to create initial administrator: {Errors}", errors);
                        }
                    }
                }
                else
                {
                    logger.LogInformation("No InitialAdmin credentials found in configuration. Skipping initial administrator seeding.");
                }

                // 3. Seed Sample Students in Development environment if database is empty
                if ((env == null || env.IsDevelopment()) && !await context.Students.AnyAsync())
                {
                    var students = new List<Student>
                    {
                        new()
                        {
                            Name = "Amit Deshmukh",
                            Email = "amit.deshmukh@enterprise.edu",
                            Age = 21,
                            Course = "Computer Science",
                            PhoneNumber = "+91 9876543210",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new()
                        {
                            Name = "Pooja Patil",
                            Email = "pooja.patil@enterprise.edu",
                            Age = 22,
                            Course = "Information Technology",
                            PhoneNumber = "+91 9876543211",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new()
                        {
                            Name = "Rahul Sharma",
                            Email = "rahul.sharma@enterprise.edu",
                            Age = 20,
                            Course = "Mechanical Engineering",
                            PhoneNumber = "+91 9876543212",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        }
                    };

                    await context.Students.AddRangeAsync(students);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Sample student records seeded for development.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database seeding.");
                throw;
            }
        }
    }
}
