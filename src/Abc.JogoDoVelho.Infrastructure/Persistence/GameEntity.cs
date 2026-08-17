namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class GameEntity
{
    public Guid Id { get; set; }

    public required string PublicCode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? FinishedAtUtc { get; set; }

    public required string Status { get; set; }

    public int? WinnerPosition { get; set; }

    public ICollection<PlayerEntity> Players { get; } = new List<PlayerEntity>();
}

