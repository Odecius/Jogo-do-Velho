using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Abc.JogoDoVelho.IntegrationTests;

public sealed class GameHttpTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;

    public GameHttpTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateJoinExistingRoomAndRejectThirdPlayer()
    {
        using var first = _factory.CreateClient();
        using var second = _factory.CreateClient();
        using var third = _factory.CreateClient();
        var created = await CreateGameAsync(first);

        using var page = await second.GetAsync(created.JoinUrl);
        using var joined = await SecurePostAsync(second, $"/api/games/{created.PublicCode}/join");
        using var rejected = await SecurePostAsync(third, $"/api/games/{created.PublicCode}/join");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task MissingGameReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        using var page = await client.GetAsync("/game/ABCDEFGH");
        using var join = await SecurePostAsync(client, "/api/games/ABCDEFGH/join");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Partida indisponível", await page.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, join.StatusCode);
    }

    [Fact]
    public async Task MutatingEndpointRequiresAntiforgeryToken()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync("/api/games", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGameIsRateLimited()
    {
        await using var factory = new FoundationWebApplicationFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? last = null;
        for (var attempt = 0; attempt < 11; attempt++)
        {
            last?.Dispose();
            last = await SecurePostAsync(client, "/api/games");
        }

        using (last)
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        }
    }

    internal static async Task<CreatedGameResponse> CreateGameAsync(HttpClient client)
    {
        using var response = await SecurePostAsync(client, "/api/games");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedGameResponse>())!;
    }

    internal static async Task<HttpResponseMessage> SecurePostAsync(HttpClient client, string url)
    {
        using var tokenResponse = await client.GetAsync("/api/antiforgery");
        var document = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", document!.RootElement.GetProperty("requestToken").GetString());
        return await client.SendAsync(request);
    }

    internal sealed record CreatedGameResponse(string PublicCode, string JoinUrl);
}
