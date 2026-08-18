using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Abc.JogoDoVelho.IntegrationTests;

public sealed class AvatarHttpTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;
    public AvatarHttpTests(FoundationWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthorizedUploadAndReadWorkButPublicCodeAloneDoesNotAuthorize()
    {
        using var player = _factory.CreateClient();
        using var anonymous = _factory.CreateClient();
        var game = await GameHttpTests.CreateGameAsync(player);

        using var uploaded = await UploadAsync(player, game.PublicCode);
        using var visible = await player.GetAsync($"/api/games/{game.PublicCode}/players/1/avatar");
        using var forbidden = await anonymous.GetAsync($"/api/games/{game.PublicCode}/players/1/avatar");

        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        Assert.Equal("image/webp", visible.Content.Headers.ContentType!.MediaType);
        Assert.True(visible.Headers.CacheControl!.Private);
        Assert.True(visible.Headers.CacheControl.NoStore);
        Assert.Equal(TimeSpan.Zero, visible.Headers.CacheControl.MaxAge);
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);
    }

    [Fact]
    public async Task UploadRequiresAntiforgeryAndValidMembership()
    {
        using var player = _factory.CreateClient();
        using var outsider = _factory.CreateClient();
        var game = await GameHttpTests.CreateGameAsync(player);
        await GameHttpTests.CreateGameAsync(outsider);
        using var form = await ArtificialFormAsync();
        using var withoutToken = await player.PostAsync($"/api/games/{game.PublicCode}/avatar", form);
        using var outsiderUpload = await UploadAsync(outsider, game.PublicCode);

        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, outsiderUpload.StatusCode);
    }

    [Fact]
    public async Task InvalidMimeIsRejectedAndUploadRateLimitApplies()
    {
        await using var factory = new FoundationWebApplicationFactory();
        using var player = factory.CreateClient();
        var game = await GameHttpTests.CreateGameAsync(player);
        using var invalid = await UploadAsync(player, game.PublicCode, "text/html");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        HttpResponseMessage? last = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            last?.Dispose();
            last = await UploadAsync(player, game.PublicCode);
        }
        using (last) Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    internal static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string publicCode,
        string contentType = "image/png")
    {
        using var tokenResponse = await client.GetAsync("/api/antiforgery");
        var document = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/games/{publicCode}/avatar");
        request.Headers.Add("X-CSRF-TOKEN", document!.RootElement.GetProperty("requestToken").GetString());
        request.Content = await ArtificialFormAsync(contentType);
        return await client.SendAsync(request);
    }

    private static async Task<MultipartFormDataContent> ArtificialFormAsync(string contentType = "image/png")
    {
        using var image = new Image<Rgba32>(24, 24, Color.Orange);
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        var bytes = new ByteArrayContent(stream.ToArray());
        bytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(bytes, "avatar", "artificial.png");
        return form;
    }
}
