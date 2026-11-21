using Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class SkillSwapDbContext : DbContext
{
    public SkillSwapDbContext(DbContextOptions<SkillSwapDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<EmailToken> EmailTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .IsRequired();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20);

        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .IsRequired()
            .HasMaxLength(20);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Profile>()
            .Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(Profile.MaxNameLength);

        modelBuilder.Entity<Profile>()
            .Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(Profile.MaxNameLength);

        modelBuilder.Entity<Profile>()
            .Property(p => p.Bio)
            .HasMaxLength(1000);

        modelBuilder.Entity<Profile>()
            .Property(p => p.AvatarUrl)
            .HasMaxLength(500);

        modelBuilder.Entity<Profile>()
            .HasOne<User>()
            .WithOne(user => user.Profile)
            .HasForeignKey<Profile>(profile => profile.UserId);

        modelBuilder.Entity<EmailToken>()
            .Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        modelBuilder.Entity<EmailToken>()
            .Property(token => token.Purpose)
            .IsRequired()
            .HasMaxLength(30);

        modelBuilder.Entity<EmailToken>()
            .Property(token => token.FailedAttempts)
            .HasDefaultValue(0);

        modelBuilder.Entity<EmailToken>()
            .HasIndex(token => token.TokenHash)
            .IsUnique();

        modelBuilder.Entity<EmailToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
