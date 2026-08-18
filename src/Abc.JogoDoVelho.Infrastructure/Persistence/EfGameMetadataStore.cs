using Microsoft.EntityFrameworkCore;

namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class EfGameMetadataStore(IDbContextFactory<AppDbContext> contextFactory) : IGameMetadataStore
{
    public async Task CreateGameAsync(
        Guid gameId,
        string publicCode,
        Guid playerId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var game = new GameEntity
        {
            Id = gameId,
            PublicCode = publicCode,
            CreatedAtUtc = createdAtUtc,
            Status = "WaitingForPlayer"
        };
        game.Players.Add(new PlayerEntity
        {
            Id = playerId,
            GameId = gameId,
            Position = 1,
            JoinedAtUtc = createdAtUtc
        });
        context.Games.Add(game);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPlayerAsync(
        Guid gameId,
        Guid playerId,
        int position,
        DateTimeOffset joinedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var game = await context.Games.SingleAsync(item => item.Id == gameId, cancellationToken);
        game.Status = "WaitingForAvatars";
        context.Players.Add(new PlayerEntity
        {
            Id = playerId,
            GameId = gameId,
            Position = position,
            JoinedAtUtc = joinedAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteGameAsync(
        Guid gameId,
        string status,
        int? winnerPosition,
        DateTimeOffset finishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var game = await context.Games.SingleAsync(item => item.Id == gameId, cancellationToken);
        game.Status = status;
        game.WinnerPosition = winnerPosition;
        game.FinishedAtUtc = finishedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
    }
}

