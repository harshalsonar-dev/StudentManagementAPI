using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                // Ensure database is created and up to date
                await context.Database.MigrateAsync();

                // 1. Seed Roles
                string[] roleNames = { "Admin", "Teacher", "Student" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        logger.LogInformation("Role created: {RoleName}", roleName);
                    }
                }

                // 2. Seed Default Admin User
                var adminEmail = "admin@studentapi.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        logger.LogInformation("Default Admin user created successfully with email: {AdminEmail}", adminEmail);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create default Admin user: {Errors}", errors);
                    }
                }

                // 3. Seed Sample Students if none exist
                if (!await context.Students.AnyAsync())
                {
                    var sampleStudents = new List<Student>
                    {
                        new Student
                        {
                            Name = "Rahul Sharma",
                            Email = "rahul.sharma@example.com",
                            Age = 21,
                            Course = "Computer Science",
                            PhoneNumber = "+91 9876543210",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Student
                        {
                            Name = "Pooja Patil",
                            Email = "pooja.patil@example.com",
                            Age = 22,
                            Course = "Information Technology",
                            PhoneNumber = "+91 9876543211",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Student
                        {
                            Name = "Amit Deshmukh",
                            Email = "amit.deshmukh@example.com",
                            Age = 20,
                            Course = "Mechanical Engineering",
                            PhoneNumber = "+91 9876543212",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        },
                        new Student
                        {
                            Name = "Sneha Kulkarni",
                            Email = "sneha.kulkarni@example.com",
                            Age = 23,
                            Course = "Computer Science",
                            PhoneNumber = "+91 9876543213",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        }
                    };

                    await context.Students.AddRangeAsync(sampleStudents);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Sample student records seeded successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}

