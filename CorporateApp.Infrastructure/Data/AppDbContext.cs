using CorporateApp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace CorporateApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Tcno).HasMaxLength(11);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Tcno).IsUnique();
            });

            // Seed data (optional)
            modelBuilder.Entity<User>().HasData(
                new User 
                { 
                    Id = 1, 
                    Name = "Admin",
                    LastName = "User",
                    Email = "admin@example.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Tcno = "12345678901",
                    Location = "Merkez",
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                }
            );
        }
    }
}
