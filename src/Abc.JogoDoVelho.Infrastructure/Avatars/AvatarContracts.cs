namespace Abc.JogoDoVelho.Infrastructure.Avatars;

public sealed record ProcessedAvatar(byte[] Content, string ContentType);
public sealed record StoredAvatar(string StorageName, string ContentType, DateTimeOffset UploadedAtUtc, DateTimeOffset ExpiresAtUtc);
public sealed record ExpiredAvatar(Guid PlayerId, Guid GameId, int Position, string StorageName);

public interface IAvatarImageProcessor
{
    Task<ProcessedAvatar> ProcessAsync(Stream input, string declaredContentType, long length,
        CancellationToken cancellationToken = default);
}

public interface IAvatarStorage
{
    Task<string> SaveAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageName, CancellationToken cancellationToken = default);
}

public interface IAvatarMetadataStore
{
    Task<string?> SetAsync(Guid playerId, string storageName, string contentType,
        DateTimeOffset uploadedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpiredAvatar>> GetExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task ClearAsync(Guid playerId, string storageName, CancellationToken cancellationToken = default);
}

public sealed class AvatarValidationException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class UnsafeAvatarPathException() : Exception("Unsafe avatar storage path.");
