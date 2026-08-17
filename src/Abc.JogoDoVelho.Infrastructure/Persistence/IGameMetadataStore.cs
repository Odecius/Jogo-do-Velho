namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public interface IGameMetadataStore
{
    Task CreateGameAsync(
        Guid gameId,
        string publicCode,
        Guid playerId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default);

    Task AddPlayerAsync(
        Guid gameId,
        Guid playerId,
        int position,
        DateTimeOffset joinedAtUtc,
        CancellationToken cancellationToken = default);

    Task CompleteGameAsync(
        Guid gameId,
        string status,
        int? winnerPosition,
        DateTimeOffset finishedAtUtc,
        CancellationToken cancellationToken = default);
}

