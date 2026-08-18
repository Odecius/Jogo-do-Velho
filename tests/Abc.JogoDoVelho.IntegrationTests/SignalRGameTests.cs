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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

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
        await UploadAvatarAsync(first.Created.PublicCode, first.Cookies);
        await UploadAvatarAsync(first.Created.PublicCode, second);
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

    [Fact]
    public async Task CompleteFlowReachesVictoryAndStartsConsentedRematch()
    {
        var first = await CreatePlayerOneAsync();
        var second = await JoinPlayerTwoAsync(first.Created.PublicCode);
        await UploadAvatarAsync(first.Created.PublicCode, first.Cookies);
        await UploadAvatarAsync(first.Created.PublicCode, second);
        await using var firstHub = CreateHub(first.Cookies); await using var secondHub = CreateHub(second);
        var states = Channel.CreateUnbounded<GameSnapshot>();
        firstHub.On<GameSnapshot>("GameStateChanged", state => states.Writer.TryWrite(state));
        await firstHub.StartAsync(); await secondHub.StartAsync();
        await firstHub.InvokeAsync("JoinGame", first.Created.PublicCode);
        await secondHub.InvokeAsync("JoinGame", first.Created.PublicCode);
        await ReadUntilAsync(states, state => state.RoomStatus == RoomStatus.Playing && state.Player2Connected);
        await firstHub.InvokeAsync("PlaceMove", 0); await secondHub.InvokeAsync("PlaceMove", 3);
        await firstHub.InvokeAsync("PlaceMove", 1); await secondHub.InvokeAsync("PlaceMove", 4);
        await firstHub.InvokeAsync("PlaceMove", 2);
        var finished = await ReadUntilAsync(states, state => state.GameStatus == GameStatus.Won);
        await firstHub.InvokeAsync("RequestRematch");
        var waiting = await ReadUntilAsync(states, state => state.YouRequestedRematch);
        await secondHub.InvokeAsync("RequestRematch");
        var rematch = await ReadUntilAsync(states, state => state.RoundNumber == 2);

        Assert.Equal(PlayerPosition.Player1, finished.Winner);
        Assert.Equal(1, finished.Player1Score);
        Assert.True(waiting.OpponentRequestedRematch is false);
        Assert.All(rematch.Board, Assert.Null);
        Assert.Equal(PlayerPosition.Player1, rematch.CurrentPlayer);
        Assert.True(rematch.Player1HasAvatar && rematch.Player2HasAvatar);
        Assert.Equal(1, rematch.Player1Score);
    }

    [Fact]
    public async Task ReconnectWithSameSessionRestoresSnapshotAndDoesNotOpenThirdSeat()
    {
        var first = await CreatePlayerOneAsync(); var second = await JoinPlayerTwoAsync(first.Created.PublicCode);
        await using (var original = CreateHub(first.Cookies))
        {
            await original.StartAsync(); await original.InvokeAsync("JoinGame", first.Created.PublicCode);
        }
        await using var refreshed = CreateHub(first.Cookies); var states = Channel.CreateUnbounded<GameSnapshot>();
        refreshed.On<GameSnapshot>("GameStateChanged", state => states.Writer.TryWrite(state));
        await refreshed.StartAsync(); await refreshed.InvokeAsync("JoinGame", first.Created.PublicCode);
        var restored = await ReadUntilAsync(states, state => state.YouAre == PlayerPosition.Player1);
        using var third = _factory.CreateClient(); using var rejected = await GameHttpTests.SecurePostAsync(third, $"/api/games/{first.Created.PublicCode}/join");

        Assert.Equal(PlayerPosition.Player1, restored.YouAre);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.NotEmpty(second.GetCookieHeader(new Uri("http://localhost")));
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

    private async Task UploadAvatarAsync(string publicCode, CookieContainer cookies)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", cookies.GetCookieHeader(new Uri("http://localhost")));
        using var tokenResponse = await client.GetAsync("/api/antiforgery");
        var token = (await tokenResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>())!
            .RootElement.GetProperty("requestToken").GetString();
        using var image = new Image<Rgba32>(32, 32, Color.Teal);
        await using var bytes = new MemoryStream();
        await image.SaveAsync(bytes, new PngEncoder());
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(content, "avatar", "artificial.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{publicCode}/avatar") { Content = form };
        request.Headers.Add("X-CSRF-TOKEN", token);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

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
