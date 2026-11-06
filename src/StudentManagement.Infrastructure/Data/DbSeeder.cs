using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

            try
            {
                // Auto migrate
                await context.Database.MigrateAsync();

                // 1. Seed Roles
                foreach (var role in AppRoles.All)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Enterprise Role '{Role}' created.", role);
                    }
                }

                // 2. Seed Default Admin
                var adminEmail = "admin@studententerprise.com";
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

                    var result = await userManager.CreateAsync(adminUser, "EnterpriseAdmin@2026!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                        logger.LogInformation("Default Enterprise Admin user created: {AdminEmail}", adminEmail);
                    }
                }

                // 3. Seed Sample Students if empty
                if (!await context.Students.AnyAsync())
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
                    logger.LogInformation("Sample student records seeded.");
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
