using Abc.JogoDoVelho.Domain;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed record GameSnapshot(string PublicCode, RoomStatus RoomStatus,
    IReadOnlyList<PlayerPosition?> Board, PlayerPosition CurrentPlayer,
    PlayerPosition? Winner, GameStatus GameStatus, PlayerPosition YouAre,
    bool Player1Connected, bool Player2Connected);
public sealed record CreatedGame(string PublicCode, string JoinUrl, string PlayerToken);
public sealed record JoinGameResult(JoinOutcome Outcome, string? PlayerToken = null);
public sealed record MoveGameResult(MoveOutcome Outcome, IReadOnlyList<RecipientSnapshot> Snapshots);
public sealed record RecipientSnapshot(string GroupName, GameSnapshot Snapshot);
public sealed record PlayerIdentity(Guid GameId, PlayerPosition Position);
