using Abc.JogoDoVelho.Infrastructure.Persistence;
using Abc.JogoDoVelho.Web.Endpoints;
using Abc.JogoDoVelho.Web.Hubs;
using Abc.JogoDoVelho.Web.Multiplayer;
using Abc.JogoDoVelho.Web.Avatars;
using Abc.JogoDoVelho.Infrastructure.Avatars;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseConnectionString.Require(builder.Configuration.GetConnectionString("Postgres"));
var knownProxyNetwork = builder.Configuration["ReverseProxy:KnownNetwork"];
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") &&
    string.IsNullOrWhiteSpace(knownProxyNetwork))
    throw new InvalidOperationException("ReverseProxy:KnownNetwork is required outside local and testing environments.");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    if (!string.IsNullOrWhiteSpace(knownProxyNetwork))
        options.KnownNetworks.Add(ParseNetwork(knownProxyNetwork));
});

builder.Services.AddPooledDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IGameMetadataStore, EfGameMetadataStore>();
builder.Services.AddOptions<AvatarOptions>()
    .Bind(builder.Configuration.GetSection(AvatarOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "Avatar storage root is required.")
    .Validate(options => options.MaximumUploadBytes is > 0 and <= 5 * 1024 * 1024,
        "Avatar upload limit must be between 1 byte and 5 MiB.")
    .Validate(options => options.MaximumDimension is >= 512 and <= 4096,
        "Avatar maximum dimension must be between 512 and 4096 pixels.")
    .Validate(options => options.OutputSize is >= 64 and <= 512,
        "Avatar output size must be between 64 and 512 pixels.")
    .ValidateOnStart();
builder.Services.PostConfigure<AvatarOptions>(options =>
{
    if (!Path.IsPathRooted(options.RootPath)) options.RootPath = Path.Combine(builder.Environment.ContentRootPath, options.RootPath);
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 6 * 1024 * 1024);
builder.Services.AddSingleton<IAvatarMetadataStore, EfAvatarMetadataStore>();
builder.Services.AddSingleton<IAvatarImageProcessor, AvatarImageProcessor>();
builder.Services.AddSingleton<IAvatarStorage, FileSystemAvatarStorage>();
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();
builder.Services.AddOptions<GameSessionOptions>()
    .Bind(builder.Configuration.GetSection(GameSessionOptions.SectionName))
    .Validate(options => options.InactivityHours is >= 1 and <= 168,
        "Game session inactivity must be between 1 and 168 hours.")
    .Validate(options => options.CleanupMinutes is >= 1 and <= 60,
        "Game session cleanup interval must be between 1 and 60 minutes.")
    .ValidateOnStart();
builder.Services.AddHostedService<GameSessionCleanupService>();
builder.Services.AddSingleton<GameSnapshotBroadcaster>();
builder.Services.AddHostedService<AvatarCleanupService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("create-game", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("join-game", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("upload-avatar", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment() || args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await using var database = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>()
        .CreateDbContextAsync();
    await database.Database.MigrateAsync();
    if (!app.Environment.IsDevelopment()) return;
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data: blob:; media-src 'self' blob:; " +
        "style-src 'self'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    context.Response.Headers.Append("Permissions-Policy", "camera=(self), microphone=(), geolocation=()");
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapGameEndpoints();
app.MapHub<GameHub>("/gameHub");
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = item => item.Tags.Contains("live") });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = item => item.Tags.Contains("ready") });

app.Run();

static Microsoft.AspNetCore.HttpOverrides.IPNetwork ParseNetwork(string cidr)
{
    var parts = cidr.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var address) ||
        !int.TryParse(parts[1], out var prefixLength) || prefixLength < 0 ||
        prefixLength > (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
        throw new InvalidOperationException("ReverseProxy:KnownNetwork must be a valid IPv4 or IPv6 CIDR.");
    return new Microsoft.AspNetCore.HttpOverrides.IPNetwork(address, prefixLength);
}

public partial class Program;
