using Abc.JogoDoVelho.Infrastructure.Avatars;
using Abc.JogoDoVelho.Web.Multiplayer;
using Microsoft.Extensions.Options;

namespace Abc.JogoDoVelho.Web.Avatars;

public sealed class AvatarCleanupService(
    IAvatarMetadataStore metadata,
    IAvatarStorage storage,
    IGameSessionManager sessions,
    GameSnapshotBroadcaster broadcaster,
    TimeProvider timeProvider,
    IOptions<AvatarOptions> options,
    ILogger<AvatarCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.CleanupMinutes), timeProvider);
        do { await RunOnceAsync(stoppingToken); }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var expired = await metadata.GetExpiredAsync(timeProvider.GetUtcNow(), cancellationToken);
        var removed = 0;
        foreach (var avatar in expired)
        {
            try
            {
                await storage.DeleteAsync(avatar.StorageName, cancellationToken);
                await metadata.ClearAsync(avatar.PlayerId, avatar.StorageName, cancellationToken);
                var snapshots = await sessions.ClearExpiredAvatarAsync(avatar.GameId, avatar.PlayerId,
                    avatar.StorageName, cancellationToken);
                await broadcaster.BroadcastAsync(snapshots, cancellationToken);
                removed++;
            }
            catch (Exception exception)
            {
                AvatarCleanupLog.ItemFailed(logger, avatar.PlayerId, exception);
            }
        }
        AvatarCleanupLog.Completed(logger, expired.Count, removed);
    }
}

internal static partial class AvatarCleanupLog
{
    [LoggerMessage(LogLevel.Information, "Avatar cleanup processed {Processed} records and removed {Removed}")]
    public static partial void Completed(ILogger logger, int processed, int removed);

    [LoggerMessage(LogLevel.Warning, "Avatar cleanup failed for player {PlayerId}")]
    public static partial void ItemFailed(ILogger logger, Guid playerId, Exception exception);
}
