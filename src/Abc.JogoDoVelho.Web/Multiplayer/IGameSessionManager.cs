using Abc.JogoDoVelho.Domain;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public interface IGameSessionManager
{
    Task<CreatedGame> CreateGameAsync(CancellationToken cancellationToken = default);
    Task<JoinGameResult> JoinGameAsync(string publicCode, string? existingToken, CancellationToken cancellationToken = default);
    bool GameExists(string publicCode);
    bool TryResolvePlayer(string? playerToken, out PlayerIdentity identity);
    Task<IReadOnlyList<RecipientSnapshot>?> ConnectAsync(string publicCode, string playerToken, string connectionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecipientSnapshot>> DisconnectAsync(string connectionId, CancellationToken cancellationToken = default);
    Task<MoveGameResult?> PlaceMoveAsync(string playerToken, int cellIndex, CancellationToken cancellationToken = default);
    Task<AvatarUpdateResult?> SetAvatarAsync(string publicCode, string playerToken, string storageName,
        string contentType, CancellationToken cancellationToken = default);
    Task<AvatarAccess?> GetAvatarAsync(string publicCode, string playerToken, PlayerPosition position,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecipientSnapshot>> ClearExpiredAvatarAsync(Guid gameId, Guid playerId, string storageName,
        CancellationToken cancellationToken = default);
    Task<RematchGameResult?> RequestRematchAsync(string playerToken, CancellationToken cancellationToken = default);
}
