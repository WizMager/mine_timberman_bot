using Microsoft.EntityFrameworkCore;
using MineTimbermanBot.Application.Duels;
using MineTimbermanBot.Application.Sessions;
using MineTimbermanBot.Infrastructure.Persistence.Entities;

namespace MineTimbermanBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserSession> Characters => Set<UserSession>();

    public DbSet<ChatMembership> ChatMemberships => Set<ChatMembership>();

    public DbSet<Duel> Duels => Set<Duel>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) => configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("Characters");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).ValueGeneratedNever();
            entity.Property(x => x.CharacterName).HasMaxLength(128);
        });

        modelBuilder.Entity<ChatMembership>(entity =>
        {
            entity.ToTable("ChatMemberships");
            entity.HasKey(x => new { x.ChatId, x.UserId });
            entity.HasIndex(x => x.ChatId);
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<Duel>(entity =>
        {
            entity.ToTable("Duels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.ChallengerName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.OpponentName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ChallengerChoice).HasConversion<int?>();
            entity.Property(x => x.OpponentChoice).HasConversion<int?>();
            entity.HasIndex(x => x.ChallengerUserId);
            entity.HasIndex(x => x.OpponentUserId);
            entity.Ignore(x => x.BothChosen);
        });
    }
}
