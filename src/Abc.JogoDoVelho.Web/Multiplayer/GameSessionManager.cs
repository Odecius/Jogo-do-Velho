using System.Collections.Concurrent;
using System.Security.Cryptography;
using Abc.JogoDoVelho.Domain;
using Abc.JogoDoVelho.Infrastructure.Persistence;
using Abc.JogoDoVelho.Infrastructure.Avatars;
using Microsoft.Extensions.Options;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed class GameSessionManager(
    IGameMetadataStore metadataStore,
    IAvatarMetadataStore avatarMetadataStore,
    TimeProvider timeProvider,
    IOptions<AvatarOptions> avatarOptions,
    ILogger<GameSessionManager> logger) : IGameSessionManager
{
    private const string CodeAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private readonly ConcurrentDictionary<string, Session> _games = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PlayerIdentity> _players = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PlayerIdentity> _connections = new(StringComparer.Ordinal);

    public async Task<CreatedGame> CreateGameAsync(CancellationToken cancellationToken = default)
    {
        Session session;
        do { session = Session.Create(GenerateCode(), timeProvider.GetUtcNow()); }
        while (!_games.TryAdd(session.PublicCode, session));

        var player = session.Players[PlayerPosition.Player1];
        try
        {
            await metadataStore.CreateGameAsync(session.Id, session.PublicCode, player.Id,
                session.CreatedAtUtc, cancellationToken);
        }
        catch
        {
            _games.TryRemove(session.PublicCode, out _);
            throw;
        }

        _players[player.Token] = new PlayerIdentity(session.Id, player.Id, player.Position);
        GameSessionLog.GameCreated(logger, session.Id);
        return new CreatedGame(session.PublicCode, $"/game/{session.PublicCode}", player.Token);
    }

    public async Task<JoinGameResult> JoinGameAsync(string publicCode, string? existingToken,
        CancellationToken cancellationToken = default)
    {
        if (!_games.TryGetValue(Normalize(publicCode), out var session))
            return new JoinGameResult(JoinOutcome.GameNotFound);

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (existingToken is not null && _players.TryGetValue(existingToken, out var identity) &&
                identity.GameId == session.Id)
                return new JoinGameResult(JoinOutcome.Success, existingToken);
            if (session.Players.ContainsKey(PlayerPosition.Player2))
                return new JoinGameResult(JoinOutcome.RoomFull);

            var joinedAt = timeProvider.GetUtcNow();
            var player = Player.Create(PlayerPosition.Player2);
            await metadataStore.AddPlayerAsync(session.Id, player.Id, (int)player.Position, joinedAt, cancellationToken);
            session.Players[player.Position] = player;
            _players[player.Token] = new PlayerIdentity(session.Id, player.Id, player.Position);
            GameSessionLog.PlayerJoined(logger, session.Id);
            return new JoinGameResult(JoinOutcome.Success, player.Token);
        }
        finally { session.Gate.Release(); }
    }

    public bool GameExists(string publicCode) => _games.ContainsKey(Normalize(publicCode));

    public bool TryResolvePlayer(string? playerToken, out PlayerIdentity identity)
    {
        if (playerToken is not null && _players.TryGetValue(playerToken, out identity!)) return true;
        identity = null!;
        return false;
    }

    public async Task<IReadOnlyList<RecipientSnapshot>?> ConnectAsync(string publicCode, string playerToken,
        string connectionId, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePlayer(playerToken, out var identity) ||
            !_games.TryGetValue(Normalize(publicCode), out var session) || identity.GameId != session.Id)
            return null;

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            session.Players[identity.Position].Connections.Add(connectionId);
            _connections[connectionId] = identity;
            GameSessionLog.ConnectionEstablished(logger, session.Id);
            return Snapshots(session);
        }
        finally { session.Gate.Release(); }
    }

    public async Task<IReadOnlyList<RecipientSnapshot>> DisconnectAsync(string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryRemove(connectionId, out var identity) || !TryGetGame(identity.GameId, out var session))
            return [];
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            session.Players[identity.Position].Connections.Remove(connectionId);
            GameSessionLog.ConnectionDisconnected(logger, session.Id);
            return Snapshots(session);
        }
        finally { session.Gate.Release(); }
    }

    public async Task<MoveGameResult?> PlaceMoveAsync(string playerToken, int cellIndex,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolvePlayer(playerToken, out var identity) || !TryGetGame(identity.GameId, out var session)) return null;
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (RoomState(session) is not RoomStatus.Playing)
                return new MoveGameResult(MoveOutcome.RoomNotReady, Snapshots(session));

            var result = session.Game.PlaceMove(identity.Position, cellIndex);
            if (result == MoveResult.Success && session.Game.Status is not GameStatus.InProgress)
            {
                await metadataStore.CompleteGameAsync(session.Id, session.Game.Status.ToString(),
                    session.Game.Winner is null ? null : (int)session.Game.Winner.Value,
                    timeProvider.GetUtcNow(), cancellationToken);
                GameSessionLog.GameCompleted(logger, session.Id, session.Game.Status);
            }
            return new MoveGameResult(Map(result), Snapshots(session));
        }
        finally { session.Gate.Release(); }
    }

    public async Task<AvatarUpdateResult?> SetAvatarAsync(string publicCode, string playerToken, string storageName,
        string contentType, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePlayer(playerToken, out var identity) ||
            !_games.TryGetValue(Normalize(publicCode), out var session) || identity.GameId != session.Id) return null;
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (RoomState(session) is RoomStatus.Playing or RoomStatus.Finished)
                return new AvatarUpdateResult(false, "GameAlreadyStarted", null, Snapshots(session));
            var now = timeProvider.GetUtcNow();
            var previous = await avatarMetadataStore.SetAsync(identity.PlayerId, storageName, contentType,
                now, now.AddHours(avatarOptions.Value.RetentionHours), cancellationToken);
            var player = session.Players[identity.Position];
            player.AvatarStorageName = storageName;
            player.AvatarContentType = contentType;
            return new AvatarUpdateResult(true, null, previous, Snapshots(session));
        }
        finally { session.Gate.Release(); }
    }

    public async Task<AvatarAccess?> GetAvatarAsync(string publicCode, string playerToken, PlayerPosition position,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolvePlayer(playerToken, out var identity) ||
            !_games.TryGetValue(Normalize(publicCode), out var session) || identity.GameId != session.Id) return null;
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!session.Players.TryGetValue(position, out var player) || player.AvatarStorageName is null) return null;
            return new AvatarAccess(player.AvatarStorageName, player.AvatarContentType!);
        }
        finally { session.Gate.Release(); }
    }

    public async Task<IReadOnlyList<RecipientSnapshot>> ClearExpiredAvatarAsync(Guid gameId, Guid playerId,
        string storageName, CancellationToken cancellationToken = default)
    {
        if (!TryGetGame(gameId, out var session)) return [];
        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            var player = session.Players.Values.FirstOrDefault(item => item.Id == playerId);
            if (player?.AvatarStorageName == storageName)
            {
                player.AvatarStorageName = null;
                player.AvatarContentType = null;
            }
            return Snapshots(session);
        }
        finally { session.Gate.Release(); }
    }

    private bool TryGetGame(Guid id, out Session session)
    {
        session = _games.Values.FirstOrDefault(item => item.Id == id)!;
        return session is not null;
    }

    private static RecipientSnapshot[] Snapshots(Session session)
    {
        var roomStatus = RoomState(session);
        var p1Connected = session.Players[PlayerPosition.Player1].Connections.Count > 0;
        var p2Connected = session.Players.TryGetValue(PlayerPosition.Player2, out var p2) && p2.Connections.Count > 0;
        return session.Players.Values.Select(player => new RecipientSnapshot(GroupName(session.Id, player.Position),
            new GameSnapshot(session.PublicCode, roomStatus, session.Game.Board.Cells.ToArray(),
                session.Game.CurrentPlayer, session.Game.Winner, session.Game.Status, player.Position,
                p1Connected, p2Connected,
                session.Players[PlayerPosition.Player1].AvatarStorageName is not null,
                p2?.AvatarStorageName is not null,
                AvatarUrl(session, PlayerPosition.Player1), AvatarUrl(session, PlayerPosition.Player2)))).ToArray();
    }

    private static RoomStatus RoomState(Session session) =>
        session.Game.Status is not GameStatus.InProgress ? RoomStatus.Finished :
        session.Players.Count < 2 ? RoomStatus.WaitingForPlayer :
        session.Players.Values.All(player => player.AvatarStorageName is not null) ? RoomStatus.Playing :
        RoomStatus.WaitingForAvatars;

    private static string? AvatarUrl(Session session, PlayerPosition position) =>
        session.Players.TryGetValue(position, out var player) && player.AvatarStorageName is not null
            ? $"/api/games/{session.PublicCode}/players/{(int)position}/avatar?v={player.AvatarStorageName[..8]}"
            : null;

    public static string GroupName(Guid gameId, PlayerPosition position) => $"Game:{gameId:N}:{position}";
    private static string Normalize(string code) => code.Trim().ToUpperInvariant();
    private static MoveOutcome Map(MoveResult result) => result switch
    {
        MoveResult.Success => MoveOutcome.Success,
        MoveResult.InvalidCell => MoveOutcome.InvalidCell,
        MoveResult.NotPlayersTurn => MoveOutcome.NotPlayersTurn,
        MoveResult.CellOccupied => MoveOutcome.CellOccupied,
        MoveResult.GameFinished => MoveOutcome.GameFinished,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
    };

    private static string GenerateCode()
    {
        Span<char> code = stackalloc char[8];
        for (var i = 0; i < code.Length; i++) code[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        return new string(code);
    }

    private sealed class Session
    {
        private Session(string code, DateTimeOffset created)
        {
            PublicCode = code; CreatedAtUtc = created;
            Players[PlayerPosition.Player1] = Player.Create(PlayerPosition.Player1);
        }
        public Guid Id { get; } = Guid.NewGuid();
        public string PublicCode { get; }
        public DateTimeOffset CreatedAtUtc { get; }
        public Game Game { get; } = new();
        public Dictionary<PlayerPosition, Player> Players { get; } = [];
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public static Session Create(string code, DateTimeOffset created) => new(code, created);
    }

    private sealed class Player
    {
        private Player(PlayerPosition position) { Position = position; }
        public Guid Id { get; } = Guid.NewGuid();
        public PlayerPosition Position { get; }
        public string Token { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        public HashSet<string> Connections { get; } = new(StringComparer.Ordinal);
        public string? AvatarStorageName { get; set; }
        public string? AvatarContentType { get; set; }
        public static Player Create(PlayerPosition position) => new(position);
    }
}

internal static partial class GameSessionLog
{
    [LoggerMessage(LogLevel.Information, "Game {GameId} created")]
    public static partial void GameCreated(ILogger logger, Guid gameId);

    [LoggerMessage(LogLevel.Information, "Player joined game {GameId}")]
    public static partial void PlayerJoined(ILogger logger, Guid gameId);

    [LoggerMessage(LogLevel.Information, "Connection established for game {GameId}")]
    public static partial void ConnectionEstablished(ILogger logger, Guid gameId);

    [LoggerMessage(LogLevel.Information, "Connection disconnected for game {GameId}")]
    public static partial void ConnectionDisconnected(ILogger logger, Guid gameId);

    [LoggerMessage(LogLevel.Information, "Game {GameId} completed with status {Status}")]
    public static partial void GameCompleted(ILogger logger, Guid gameId, GameStatus status);
}
