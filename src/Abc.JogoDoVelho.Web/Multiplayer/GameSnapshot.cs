using Abc.JogoDoVelho.Domain;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed record GameSnapshot(string PublicCode, RoomStatus RoomStatus,
    IReadOnlyList<PlayerPosition?> Board, PlayerPosition CurrentPlayer,
    PlayerPosition? Winner, GameStatus GameStatus, PlayerPosition YouAre,
    bool Player1Connected, bool Player2Connected, bool Player1HasAvatar, bool Player2HasAvatar,
    string? Player1AvatarUrl, string? Player2AvatarUrl, int Player1Score, int Player2Score,
    int Draws, bool YouRequestedRematch, bool OpponentRequestedRematch, int RoundNumber);
public sealed record CreatedGame(string PublicCode, string JoinUrl, string PlayerToken);
public sealed record JoinGameResult(JoinOutcome Outcome, string? PlayerToken = null);
public sealed record MoveGameResult(MoveOutcome Outcome, IReadOnlyList<RecipientSnapshot> Snapshots);
public sealed record RecipientSnapshot(string GroupName, GameSnapshot Snapshot);
public sealed record PlayerIdentity(Guid GameId, Guid PlayerId, PlayerPosition Position);
public sealed record AvatarUpdateResult(bool Success, string? Error, string? PreviousStorageName,
    IReadOnlyList<RecipientSnapshot> Snapshots);
public sealed record AvatarAccess(string StorageName, string ContentType);
public sealed record RematchGameResult(bool Accepted, string? Error, IReadOnlyList<RecipientSnapshot> Snapshots);
