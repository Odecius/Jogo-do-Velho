using Abc.JogoDoVelho.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed class GameSnapshotBroadcaster(IHubContext<GameHub> hub)
{
    public Task BroadcastAsync(IReadOnlyList<RecipientSnapshot> snapshots, CancellationToken cancellationToken = default) =>
        Task.WhenAll(snapshots.Select(item => hub.Clients.Group(item.GroupName)
            .SendAsync("GameStateChanged", item.Snapshot, cancellationToken)));
}
