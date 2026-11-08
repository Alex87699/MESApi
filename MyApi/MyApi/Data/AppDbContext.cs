using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(user => user.Id);
                entity.Property(user => user.Name).HasMaxLength(50).IsRequired();
                entity.Property(user => user.Email).HasMaxLength(100).IsRequired();
                entity.HasIndex(user => user.Email).IsUnique();
                entity.Property(user => user.Phone).HasMaxLength(20);
                entity.Property(user => user.CreatedAt).IsRequired();
            });
        }
    }
}
