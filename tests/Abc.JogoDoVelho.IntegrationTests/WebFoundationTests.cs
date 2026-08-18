using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Abc.JogoDoVelho.Infrastructure.Persistence;
using Abc.JogoDoVelho.Infrastructure.Avatars;

namespace Abc.JogoDoVelho.IntegrationTests;

public sealed class WebFoundationTests : IClassFixture<FoundationWebApplicationFactory>
{
    private readonly FoundationWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebFoundationTests(FoundationWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task HealthReturnsHealthyWithoutDatabaseProbe()
    {
        using var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task HomeReturnsFoundationPageAndSecurityHeaders()
    {
        using var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        Assert.Contains("Criar partida", content, StringComparison.Ordinal);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-inline", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.Ordinal);
        Assert.DoesNotContain(" *", csp, StringComparison.Ordinal);
        Assert.Equal("camera=(self), microphone=(), geolocation=()",
            response.Headers.GetValues("Permissions-Policy").Single());
    }

    [Fact]
    public void ForwardedHeadersTrustOnlyConfiguredNetworkAndOneProxyHop()
    {
        var options = _factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Contains(options.KnownNetworks, network => network.PrefixLength == 8);
    }
}

public sealed class FoundationWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Postgres",
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1");
        builder.UseSetting("ReverseProxy:KnownNetwork", "127.0.0.0/8");
        builder.UseSetting("AvatarStorage:RootPath", Path.Combine(Path.GetTempPath(), "abc-jogo-tests", Guid.NewGuid().ToString("N")));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGameMetadataStore>();
            services.AddSingleton<IGameMetadataStore, NoOpGameMetadataStore>();
            services.RemoveAll<IAvatarMetadataStore>();
            services.AddSingleton<IAvatarMetadataStore, NoOpAvatarMetadataStore>();
        });
    }
}

internal sealed class NoOpAvatarMetadataStore : IAvatarMetadataStore
{
    private readonly Dictionary<Guid, string> _names = [];
    public Task<string?> SetAsync(Guid playerId, string storageName, string contentType,
        DateTimeOffset uploadedAtUtc, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        _names.Remove(playerId, out var previous); _names[playerId] = storageName;
        return Task.FromResult<string?>(previous);
    }
    public Task<IReadOnlyList<ExpiredAvatar>> GetExpiredAsync(DateTimeOffset now,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ExpiredAvatar>>([]);
    public Task ClearAsync(Guid playerId, string storageName,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class NoOpGameMetadataStore : IGameMetadataStore
{
    public Task CreateGameAsync(Guid gameId, string publicCode, Guid playerId,
        DateTimeOffset createdAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AddPlayerAsync(Guid gameId, Guid playerId, int position,
        DateTimeOffset joinedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CompleteGameAsync(Guid gameId, string status, int? winnerPosition,
        DateTimeOffset finishedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ExpireGameAsync(Guid gameId, DateTimeOffset expiredAtUtc,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

