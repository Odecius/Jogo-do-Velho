using Abc.JogoDoVelho.Domain;
using Abc.JogoDoVelho.Infrastructure.Persistence;
using Abc.JogoDoVelho.Infrastructure.Avatars;
using Abc.JogoDoVelho.Web.Multiplayer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Abc.JogoDoVelho.Tests;

public sealed class GameSessionManagerTests
{
    [Fact]
    public async Task CreateGameCreatesPlayer1AndCryptographicPublicCode()
    {
        var manager = CreateManager();

        var created = await manager.CreateGameAsync();

        Assert.Matches("^[2-9A-HJ-NP-Z]{8}$", created.PublicCode);
        Assert.Equal($"/game/{created.PublicCode}", created.JoinUrl);
        Assert.True(manager.TryResolvePlayer(created.PlayerToken, out var player));
        Assert.Equal(PlayerPosition.Player1, player.Position);
    }

    [Fact]
    public async Task SecondPlayerJoinsAndThirdPlayerIsRejected()
    {
        var manager = CreateManager();
        var created = await manager.CreateGameAsync();

        var second = await manager.JoinGameAsync(created.PublicCode.ToLowerInvariant(), null);
        var third = await manager.JoinGameAsync(created.PublicCode, null);

        Assert.Equal(JoinOutcome.Success, second.Outcome);
        Assert.Equal(JoinOutcome.RoomFull, third.Outcome);
        Assert.True(manager.TryResolvePlayer(second.PlayerToken, out var player));
        Assert.Equal(PlayerPosition.Player2, player.Position);
    }

    [Fact]
    public async Task ExistingPlayerCanJoinSameRoomAgain()
    {
        var manager = CreateManager();
        var created = await manager.CreateGameAsync();

        var result = await manager.JoinGameAsync(created.PublicCode, created.PlayerToken);

        Assert.Equal(JoinOutcome.Success, result.Outcome);
        Assert.Equal(created.PlayerToken, result.PlayerToken);
    }

    [Fact]
    public async Task MissingGameAndInvalidSessionAreRejected()
    {
        var manager = CreateManager();

        Assert.Equal(JoinOutcome.GameNotFound, (await manager.JoinGameAsync("ABCDEFGH", null)).Outcome);
        Assert.Null(await manager.PlaceMoveAsync("invalid-session", 0));
    }

    [Fact]
    public async Task SessionFromAnotherGameCannotConnect()
    {
        var manager = CreateManager();
        var first = await manager.CreateGameAsync();
        var second = await manager.CreateGameAsync();

        var snapshots = await manager.ConnectAsync(second.PublicCode, first.PlayerToken, "connection-1");

        Assert.Null(snapshots);
    }

    [Fact]
    public async Task MoveIsDelegatedToDomainAndSnapshotIsPersonalized()
    {
        var manager = CreateManager();
        var first = await manager.CreateGameAsync();
        var second = await manager.JoinGameAsync(first.PublicCode, null);
        await AddBothAvatars(manager, first, second);
        await manager.ConnectAsync(first.PublicCode, first.PlayerToken, "p1");
        var snapshots = await manager.ConnectAsync(first.PublicCode, second.PlayerToken!, "p2");

        var result = await manager.PlaceMoveAsync(first.PlayerToken, 4);

        Assert.Equal(MoveOutcome.Success, result!.Outcome);
        Assert.Equal(PlayerPosition.Player1, result.Snapshots[0].Snapshot.Board[4]);
        Assert.Equal(PlayerPosition.Player2, result.Snapshots[0].Snapshot.CurrentPlayer);
        Assert.Contains(snapshots!, item => item.Snapshot.YouAre == PlayerPosition.Player1);
        Assert.Contains(snapshots!, item => item.Snapshot.YouAre == PlayerPosition.Player2);
        Assert.All(snapshots!, item => Assert.True(item.Snapshot.Player1Connected && item.Snapshot.Player2Connected));
    }

    [Fact]
    public async Task MoveBeforeSecondPlayerJoinsIsRejected()
    {
        var manager = CreateManager();
        var created = await manager.CreateGameAsync();

        var result = await manager.PlaceMoveAsync(created.PlayerToken, 0);

        Assert.Equal(MoveOutcome.RoomNotReady, result!.Outcome);
    }

    [Fact]
    public async Task ConcurrentMovesBySamePlayerAllowOnlyOneMutation()
    {
        var manager = CreateManager();
        var first = await manager.CreateGameAsync();
        var joined = await manager.JoinGameAsync(first.PublicCode, null);
        await AddBothAvatars(manager, first, joined);

        var results = await Task.WhenAll(
            manager.PlaceMoveAsync(first.PlayerToken, 0),
            manager.PlaceMoveAsync(first.PlayerToken, 1));

        Assert.Single(results, result => result!.Outcome == MoveOutcome.Success);
        Assert.Single(results, result => result!.Outcome == MoveOutcome.NotPlayersTurn);
        var board = results.Single(result => result!.Outcome == MoveOutcome.NotPlayersTurn)!.Snapshots[0].Snapshot.Board;
        Assert.Single(board, cell => cell == PlayerPosition.Player1);
    }

    [Fact]
    public async Task DisconnectUpdatesPresenceWithoutRemovingPlayer()
    {
        var manager = CreateManager();
        var first = await manager.CreateGameAsync();
        var second = await manager.JoinGameAsync(first.PublicCode, null);
        await manager.ConnectAsync(first.PublicCode, first.PlayerToken, "p1");
        await manager.ConnectAsync(first.PublicCode, second.PlayerToken!, "p2");

        var disconnected = await manager.DisconnectAsync("p2");
        var reconnected = await manager.ConnectAsync(first.PublicCode, second.PlayerToken!, "p2-new");

        Assert.All(disconnected, item => Assert.False(item.Snapshot.Player2Connected));
        Assert.All(reconnected!, item => Assert.True(item.Snapshot.Player2Connected));
    }

    [Fact]
    public async Task RematchRequiresFinishedGameAndConsentFromBothPlayers()
    {
        var (manager, first, second) = await ReadyGameAsync();

        var early = await manager.RequestRematchAsync(first.PlayerToken);
        await PlayPlayerOneWinAsync(manager, first.PlayerToken, second.PlayerToken!);
        var requested = await manager.RequestRematchAsync(first.PlayerToken);
        var duplicate = await manager.RequestRematchAsync(first.PlayerToken);

        Assert.False(early!.Accepted);
        Assert.All(requested!.Snapshots, item => Assert.Equal(RoomStatus.Finished, item.Snapshot.RoomStatus));
        Assert.All(requested.Snapshots, item => Assert.Equal(1, item.Snapshot.Player1Score));
        Assert.Single(duplicate!.Snapshots, item => item.Snapshot.YouAre == PlayerPosition.Player1 && item.Snapshot.YouRequestedRematch);

        var restarted = await manager.RequestRematchAsync(second.PlayerToken!);

        Assert.All(restarted!.Snapshots, item => Assert.Equal(RoomStatus.Playing, item.Snapshot.RoomStatus));
        Assert.All(restarted.Snapshots, item => Assert.All(item.Snapshot.Board, Assert.Null));
        Assert.All(restarted.Snapshots, item => Assert.Equal(PlayerPosition.Player1, item.Snapshot.CurrentPlayer));
        Assert.All(restarted.Snapshots, item => Assert.True(item.Snapshot.Player1HasAvatar && item.Snapshot.Player2HasAvatar));
        Assert.All(restarted.Snapshots, item => Assert.False(item.Snapshot.YouRequestedRematch || item.Snapshot.OpponentRequestedRematch));
        Assert.All(restarted.Snapshots, item => Assert.Equal(2, item.Snapshot.RoundNumber));
    }

    [Fact]
    public async Task SessionScoreCountsPlayerTwoWinAndDraw()
    {
        var (manager, first, second) = await ReadyGameAsync();
        await manager.PlaceMoveAsync(first.PlayerToken, 0); await manager.PlaceMoveAsync(second.PlayerToken!, 3);
        await manager.PlaceMoveAsync(first.PlayerToken, 1); await manager.PlaceMoveAsync(second.PlayerToken!, 4);
        await manager.PlaceMoveAsync(first.PlayerToken, 8); var won = await manager.PlaceMoveAsync(second.PlayerToken!, 5);
        Assert.All(won!.Snapshots, item => Assert.Equal(1, item.Snapshot.Player2Score));
        await manager.RequestRematchAsync(first.PlayerToken); await manager.RequestRematchAsync(second.PlayerToken!);
        var moves = new[] { (first.PlayerToken, 0), (second.PlayerToken!, 1), (first.PlayerToken, 2),
            (second.PlayerToken!, 4), (first.PlayerToken, 3), (second.PlayerToken!, 5),
            (first.PlayerToken, 7), (second.PlayerToken!, 6), (first.PlayerToken, 8) };
        MoveGameResult? drawn = null;
        foreach (var (token, cell) in moves) drawn = await manager.PlaceMoveAsync(token, cell);
        Assert.All(drawn!.Snapshots, item => Assert.Equal(1, item.Snapshot.Draws));
        Assert.All(drawn.Snapshots, item => Assert.Equal(GameStatus.Draw, item.Snapshot.GameStatus));
    }

    private static async Task<(GameSessionManager Manager, CreatedGame First, JoinGameResult Second)> ReadyGameAsync()
    {
        var manager = CreateManager(); var first = await manager.CreateGameAsync();
        var second = await manager.JoinGameAsync(first.PublicCode, null); await AddBothAvatars(manager, first, second);
        return (manager, first, second);
    }

    private static async Task PlayPlayerOneWinAsync(GameSessionManager manager, string first, string second)
    {
        await manager.PlaceMoveAsync(first, 0); await manager.PlaceMoveAsync(second, 3);
        await manager.PlaceMoveAsync(first, 1); await manager.PlaceMoveAsync(second, 4);
        await manager.PlaceMoveAsync(first, 2);
    }

    [Fact]
    public async Task InactiveGameExpirationRemovesRoomAndSessionToken()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero));
        var manager = CreateManager(clock); var game = await manager.CreateGameAsync();
        clock.Advance(TimeSpan.FromHours(25));

        var removed = await manager.ExpireInactiveGamesAsync(clock.GetUtcNow().AddHours(-24));

        Assert.Equal(1, removed);
        Assert.False(manager.GameExists(game.PublicCode));
        Assert.False(manager.TryResolvePlayer(game.PlayerToken, out _));
        Assert.Equal(0, await manager.ExpireInactiveGamesAsync(clock.GetUtcNow()));
    }

    [Fact]
    public async Task ManipulatedMovesAndPrematureRematchDoNotCorruptGame()
    {
        var (manager, first, second) = await ReadyGameAsync();
        foreach (var index in new[] { -1, 9, 999999 })
            Assert.Equal(MoveOutcome.InvalidCell, (await manager.PlaceMoveAsync(first.PlayerToken, index))!.Outcome);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(index => manager.PlaceMoveAsync(first.PlayerToken, index % 9)));
        Assert.Single(attempts, item => item!.Outcome == MoveOutcome.Success);
        Assert.False((await manager.RequestRematchAsync(second.PlayerToken!))!.Accepted);
        var board = attempts.Last()!.Snapshots[0].Snapshot.Board;
        Assert.Single(board, cell => cell == PlayerPosition.Player1);
    }

    private static async Task AddBothAvatars(GameSessionManager manager, CreatedGame first, JoinGameResult second)
    {
        await manager.SetAvatarAsync(first.PublicCode, first.PlayerToken, "one.webp", "image/webp");
        await manager.SetAvatarAsync(first.PublicCode, second.PlayerToken!, "two.webp", "image/webp");
    }

    private static GameSessionManager CreateManager(TimeProvider? timeProvider = null) => new(
        new FakeMetadataStore(), new FakeAvatarMetadataStore(), new FakeAvatarStorage(), timeProvider ?? TimeProvider.System, Options.Create(new AvatarOptions()),
        NullLogger<GameSessionManager>.Instance);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public void Advance(TimeSpan value) => utcNow = utcNow.Add(value);
    }

    private sealed class FakeAvatarStorage : IAvatarStorage
    {
        public Task<string> SaveAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default) =>
            Task.FromResult("unused.webp");
        public Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string storageName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAvatarMetadataStore : IAvatarMetadataStore
    {
        public Task<string?> SetAsync(Guid playerId, string storageName, string contentType,
            DateTimeOffset uploadedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
        public Task<IReadOnlyList<ExpiredAvatar>> GetExpiredAsync(DateTimeOffset now,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExpiredAvatar>>([]);
        public Task ClearAsync(Guid playerId, string storageName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMetadataStore : IGameMetadataStore
    {
        public Task CreateGameAsync(Guid gameId, string publicCode, Guid playerId,
            DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddPlayerAsync(Guid gameId, Guid playerId, int position,
            DateTimeOffset joinedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CompleteGameAsync(Guid gameId, string status, int? winnerPosition,
            DateTimeOffset finishedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExpireGameAsync(Guid gameId, DateTimeOffset expiredAtUtc,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
