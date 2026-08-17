using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Abc.JogoDoVelho.Domain;
using Abc.JogoDoVelho.Web.Multiplayer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.JogoDoVelho.IntegrationTests;

public sealed class SignalRGameTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;

    public SignalRGameTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task TwoPlayersExchangeMovesAndReceiveServerSnapshots()
    {
        var first = await CreatePlayerOneAsync();
        var second = await JoinPlayerTwoAsync(first.Created.PublicCode);
        await using var firstHub = CreateHub(first.Cookies);
        await using var secondHub = CreateHub(second);
        var firstStates = Channel.CreateUnbounded<GameSnapshot>();
        var secondStates = Channel.CreateUnbounded<GameSnapshot>();
        var rejections = Channel.CreateUnbounded<string>();
        firstHub.On<GameSnapshot>("GameStateChanged", state => firstStates.Writer.TryWrite(state));
        secondHub.On<GameSnapshot>("GameStateChanged", state => secondStates.Writer.TryWrite(state));
        secondHub.On<string>("MoveRejected", outcome => rejections.Writer.TryWrite(outcome));

        await firstHub.StartAsync();
        await secondHub.StartAsync();
        await firstHub.InvokeAsync("JoinGame", first.Created.PublicCode);
        await secondHub.InvokeAsync("JoinGame", first.Created.PublicCode);
        await ReadUntilAsync(secondStates, state => state.Player1Connected && state.Player2Connected);

        await firstHub.InvokeAsync("PlaceMove", 0);
        var firstMove = await ReadUntilAsync(secondStates, state => state.Board[0] == PlayerPosition.Player1);
        await secondHub.InvokeAsync("PlaceMove", 1);
        var secondMove = await ReadUntilAsync(firstStates, state => state.Board[1] == PlayerPosition.Player2);
        await secondHub.InvokeAsync("PlaceMove", 2);

        Assert.Equal(PlayerPosition.Player2, firstMove.CurrentPlayer);
        Assert.Equal(PlayerPosition.Player1, secondMove.CurrentPlayer);
        Assert.Equal(MoveOutcome.NotPlayersTurn.ToString(), await ReadAsync(rejections));
    }

    [Fact]
    public async Task SessionWithoutAuthorizationCannotJoinGameHubGroup()
    {
        var first = await CreatePlayerOneAsync();
        await using var unauthorized = CreateHub(new CookieContainer());
        await unauthorized.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => unauthorized.InvokeAsync("JoinGame", first.Created.PublicCode));

        Assert.Contains("SessionInvalid", exception.Message, StringComparison.Ordinal);
    }

    private async Task<(GameHttpTests.CreatedGameResponse Created, CookieContainer Cookies)> CreatePlayerOneAsync()
    {
        using var client = _factory.CreateClient();
        using var response = await GameHttpTests.SecurePostAsync(client, "/api/games");
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<GameHttpTests.CreatedGameResponse>())!;
        return (created, ReadCookies(response));
    }

    private async Task<CookieContainer> JoinPlayerTwoAsync(string publicCode)
    {
        using var client = _factory.CreateClient();
        using var response = await GameHttpTests.SecurePostAsync(client, $"/api/games/{publicCode}/join");
        response.EnsureSuccessStatusCode();
        return ReadCookies(response);
    }

    private HubConnection CreateHub(CookieContainer cookies) => new HubConnectionBuilder()
        .WithUrl("http://localhost/gameHub", options =>
        {
            options.Cookies = cookies;
            var cookieHeader = cookies.GetCookieHeader(new Uri("http://localhost"));
            if (cookieHeader.Length > 0) options.Headers.Add("Cookie", cookieHeader);
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        })
        .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .Build();

    private static CookieContainer ReadCookies(HttpResponseMessage response)
    {
        var container = new CookieContainer();
        foreach (var header in response.Headers.GetValues("Set-Cookie"))
            container.SetCookies(new Uri("http://localhost"), header);
        return container;
    }

    private static async Task<GameSnapshot> ReadUntilAsync(
        Channel<GameSnapshot> channel,
        Func<GameSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (await channel.Reader.WaitToReadAsync(timeout.Token))
            while (channel.Reader.TryRead(out var snapshot))
                if (predicate(snapshot)) return snapshot;
        throw new TimeoutException("Expected SignalR snapshot was not received.");
    }

    private static async Task<T> ReadAsync<T>(Channel<T> channel)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await channel.Reader.ReadAsync(timeout.Token);
    }
}
