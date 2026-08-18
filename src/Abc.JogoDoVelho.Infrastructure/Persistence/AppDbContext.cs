using Microsoft.EntityFrameworkCore;

namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GameEntity> Games => Set<GameEntity>();

    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(game => game.Id);
            entity.Property(game => game.PublicCode).HasMaxLength(8).IsRequired();
            entity.HasIndex(game => game.PublicCode).IsUnique();
            entity.Property(game => game.Status).HasMaxLength(32).IsRequired();
            entity.HasMany(game => game.Players)
                .WithOne(player => player.Game)
                .HasForeignKey(player => player.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(player => player.Id);
            entity.HasIndex(player => new { player.GameId, player.Position }).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_players_position", "\"Position\" IN (1, 2)"));
            entity.Property(player => player.AvatarStorageName).HasMaxLength(64);
            entity.Property(player => player.AvatarContentType).HasMaxLength(32);
            entity.HasIndex(player => player.AvatarExpiresAtUtc);
        });
    }
}

