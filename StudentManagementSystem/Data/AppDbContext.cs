using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;

namespace StudentManagementAPI.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.Course)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
            });
        }
    }
}
