using Microsoft.Extensions.Options;

namespace Abc.JogoDoVelho.Infrastructure.Avatars;

public sealed class FileSystemAvatarStorage : IAvatarStorage
{
    private readonly string _root;

    public FileSystemAvatarStorage(IOptions<AvatarOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
        RejectReparsePoint(_root);
    }

    public async Task<string> SaveAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var storageName = $"{Guid.NewGuid():N}.webp";
        var path = Resolve(storageName);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(content, cancellationToken);
        return storageName;
    }

    public Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageName);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        RejectReparsePoint(path);
        return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan));
    }

    public Task DeleteAsync(string storageName, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageName);
        if (File.Exists(path))
        {
            RejectReparsePoint(path);
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string Resolve(string storageName)
    {
        if (string.IsNullOrWhiteSpace(storageName) || Path.GetFileName(storageName) != storageName ||
            !storageName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) throw new UnsafeAvatarPathException();
        var path = Path.GetFullPath(Path.Combine(_root, storageName));
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new UnsafeAvatarPathException();
        return path;
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new UnsafeAvatarPathException();
    }
}
