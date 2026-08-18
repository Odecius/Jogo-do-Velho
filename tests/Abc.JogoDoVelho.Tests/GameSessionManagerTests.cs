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

    private static async Task AddBothAvatars(GameSessionManager manager, CreatedGame first, JoinGameResult second)
    {
        await manager.SetAvatarAsync(first.PublicCode, first.PlayerToken, "one.webp", "image/webp");
        await manager.SetAvatarAsync(first.PublicCode, second.PlayerToken!, "two.webp", "image/webp");
    }

    private static GameSessionManager CreateManager() => new(
        new FakeMetadataStore(), new FakeAvatarMetadataStore(), TimeProvider.System, Options.Create(new AvatarOptions()),
        NullLogger<GameSessionManager>.Instance);

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
    }
}
