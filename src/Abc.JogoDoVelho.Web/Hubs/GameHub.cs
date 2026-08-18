using Abc.JogoDoVelho.Web.Multiplayer;
using Microsoft.AspNetCore.SignalR;

namespace Abc.JogoDoVelho.Web.Hubs;

public sealed class GameHub(IGameSessionManager sessions) : Hub
{
    public async Task JoinGame(string publicCode)
    {
        var token = Context.GetHttpContext()?.Request.Cookies[PlayerSessionCookie.Name];
        if (token is null || !sessions.TryResolvePlayer(token, out var identity))
            throw new HubException("SessionInvalid");

        var snapshots = await sessions.ConnectAsync(publicCode, token, Context.ConnectionId, Context.ConnectionAborted);
        if (snapshots is null) throw new HubException("GameAccessDenied");

        await Groups.AddToGroupAsync(Context.ConnectionId,
            GameSessionManager.GroupName(identity.GameId, identity.Position), Context.ConnectionAborted);
        await BroadcastAsync(snapshots);
    }

    public async Task PlaceMove(int cellIndex)
    {
        var token = Context.GetHttpContext()?.Request.Cookies[PlayerSessionCookie.Name];
        if (token is null) throw new HubException("SessionInvalid");
        var result = await sessions.PlaceMoveAsync(token, cellIndex, Context.ConnectionAborted);
        if (result is null) throw new HubException("SessionInvalid");
        if (result.Outcome is not MoveOutcome.Success)
        {
            await Clients.Caller.SendAsync("MoveRejected", result.Outcome.ToString(), Context.ConnectionAborted);
            return;
        }
        await BroadcastAsync(result.Snapshots);
    }

    public async Task RequestRematch()
    {
        var token = Context.GetHttpContext()?.Request.Cookies[PlayerSessionCookie.Name];
        if (token is null) throw new HubException("SessionInvalid");
        var result = await sessions.RequestRematchAsync(token, Context.ConnectionAborted);
        if (result is null) throw new HubException("SessionInvalid");
        if (!result.Accepted)
        {
            await Clients.Caller.SendAsync("RematchRejected", result.Error, Context.ConnectionAborted);
            return;
        }
        await BroadcastAsync(result.Snapshots);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var snapshots = await sessions.DisconnectAsync(Context.ConnectionId);
        await BroadcastAsync(snapshots);
        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastAsync(IReadOnlyList<RecipientSnapshot> snapshots) => Task.WhenAll(
        snapshots.Select(item => Clients.Group(item.GroupName)
            .SendAsync("GameStateChanged", item.Snapshot, Context.ConnectionAborted)));
}
