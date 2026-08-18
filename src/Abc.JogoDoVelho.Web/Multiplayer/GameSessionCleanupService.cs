using Microsoft.Extensions.Options;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed class GameSessionCleanupService(
    IGameSessionManager sessions,
    TimeProvider timeProvider,
    IOptions<GameSessionOptions> options,
    ILogger<GameSessionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.CleanupMinutes), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var cutoff = timeProvider.GetUtcNow().AddHours(-options.Value.InactivityHours);
                var removed = await sessions.ExpireInactiveGamesAsync(cutoff, stoppingToken);
                GameSessionCleanupLog.Completed(logger, removed);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                GameSessionCleanupLog.Failed(logger, exception);
            }
        }
    }
}

internal static partial class GameSessionCleanupLog
{
    [LoggerMessage(LogLevel.Information, "Expired game session cleanup removed {Removed} rooms")]
    public static partial void Completed(ILogger logger, int removed);

    [LoggerMessage(LogLevel.Warning, "Expired game session cleanup failed")]
    public static partial void Failed(ILogger logger, Exception exception);
}
