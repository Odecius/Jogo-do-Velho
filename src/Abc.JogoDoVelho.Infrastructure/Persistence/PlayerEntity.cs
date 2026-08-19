namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class PlayerEntity
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public int Position { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; }

    public string? AvatarStorageName { get; set; }

    public string? AvatarContentType { get; set; }

    public DateTimeOffset? AvatarUploadedAtUtc { get; set; }

    public DateTimeOffset? AvatarExpiresAtUtc { get; set; }

    public GameEntity Game { get; set; } = null!;
}

