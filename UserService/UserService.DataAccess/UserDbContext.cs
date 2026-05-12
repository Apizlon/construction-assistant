using Microsoft.EntityFrameworkCore;
using UserService.Application.Models;

namespace UserService.DataAccess;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.RegistrationDate)
                .IsRequired();

            // Store enum as string in database, not as integer
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}

