using gymappAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace gymappAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    public DbSet<Workout> Workouts { get; set; }

    public DbSet<Label> Labels { get; set; }

    public DbSet<Food> Foods { get; set; }

    public DbSet<LoggedFood> LoggedFoods { get; set; }

    public DbSet<FriendRequest> FriendRequests { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Dates are stored as they arrive, without a time zone. Npgsql only accepts DateTimes of a certain
    // kind for each column type, so the kind is dropped before saving.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<UnspecifiedKindConverter>();
    }

    private class UnspecifiedKindConverter() : ValueConverter<DateTime, DateTime>(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => v);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsNpgsql())
        {
            // Case-insensitive text, so "Zdx" and "zdx" are the same username.
            modelBuilder.HasPostgresExtension("citext");
            modelBuilder.Entity<User>().Property(u => u.Username).HasColumnType("citext");
        }

        // Configure Workout → Label with cascade delete
        modelBuilder.Entity<Workout>()
            .HasMany(w => w.Labels)
            .WithOne() // no reverse nav property in Label
            .HasForeignKey("WorkoutId") // EF inferred it; make it explicit
            .OnDelete(DeleteBehavior.Cascade);

        // Usernames are unique, ignoring case (see citext above).
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Username).HasMaxLength(50);
            user.HasIndex(u => u.Username).IsUnique();
        });

        // Message times are saved in UTC, but the column doesn't keep that. Mark them as UTC when reading,
        // so they reach the client with a "Z" and are shown in the reader's time zone.
        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.Timestamp)
            .HasConversion(v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // SHA-256 hex is 64 characters.
        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.Property(t => t.TokenHash).HasMaxLength(64);
            token.HasIndex(t => t.TokenHash).IsUnique();
        });
    }
}
