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

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.Tcno)
                      .HasMaxLength(11)
                      .IsRequired();
                // Sadece default değerler ve index'ler
                entity.Property(e => e.RoleId).HasDefaultValue(2);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Index'ler
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Tcno).IsUnique();
            });


        }
    }
}