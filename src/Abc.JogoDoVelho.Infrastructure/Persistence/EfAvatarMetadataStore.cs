using Abc.JogoDoVelho.Infrastructure.Avatars;
using Microsoft.EntityFrameworkCore;

namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class EfAvatarMetadataStore(IDbContextFactory<AppDbContext> contextFactory) : IAvatarMetadataStore
{
    public async Task<string?> SetAsync(Guid playerId, string storageName, string contentType,
        DateTimeOffset uploadedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var player = await context.Players.SingleAsync(item => item.Id == playerId, cancellationToken);
        var previous = player.AvatarStorageName;
        player.AvatarStorageName = storageName;
        player.AvatarContentType = contentType;
        player.AvatarUploadedAtUtc = uploadedAtUtc;
        player.AvatarExpiresAtUtc = expiresAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        var allPlayersReady = await context.Players.CountAsync(item => item.GameId == player.GameId, cancellationToken) == 2 &&
            await context.Players.AllAsync(item => item.GameId != player.GameId || item.AvatarStorageName != null, cancellationToken);
        if (allPlayersReady)
        {
            var game = await context.Games.SingleAsync(item => item.Id == player.GameId, cancellationToken);
            game.Status = "Playing";
            game.StartedAtUtc ??= uploadedAtUtc;
            await context.SaveChangesAsync(cancellationToken);
        }
        return previous;
    }

    public async Task<IReadOnlyList<ExpiredAvatar>> GetExpiredAsync(DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Players.AsNoTracking()
            .Where(item => item.AvatarStorageName != null && item.AvatarExpiresAtUtc <= now)
            .Select(item => new ExpiredAvatar(item.Id, item.GameId, item.Position, item.AvatarStorageName!))
            .ToListAsync(cancellationToken);
    }

    public async Task ClearAsync(Guid playerId, string storageName, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var player = await context.Players.SingleOrDefaultAsync(item => item.Id == playerId, cancellationToken);
        if (player is null || player.AvatarStorageName != storageName) return;
        player.AvatarStorageName = null;
        player.AvatarContentType = null;
        player.AvatarUploadedAtUtc = null;
        player.AvatarExpiresAtUtc = null;
        await context.SaveChangesAsync(cancellationToken);
    }
}
