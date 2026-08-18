using System.Text.RegularExpressions;
using Abc.JogoDoVelho.Web.Multiplayer;
using Abc.JogoDoVelho.Infrastructure.Avatars;
using Abc.JogoDoVelho.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Abc.JogoDoVelho.Web.Endpoints;

public static partial class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { requestToken = tokens.RequestToken });
        });

        endpoints.MapPost("/api/games", async (HttpContext context, IAntiforgery antiforgery,
            IGameSessionManager sessions, IWebHostEnvironment environment, CancellationToken cancellationToken) =>
        {
            if (!await IsAntiforgeryValidAsync(context, antiforgery))
                return Results.BadRequest(new { error = "AntiforgeryValidationFailed" });
            var created = await sessions.CreateGameAsync(cancellationToken);
            SetPlayerCookie(context, environment, created.PlayerToken);
            return Results.Created(created.JoinUrl, new { created.PublicCode, created.JoinUrl });
        }).RequireRateLimiting("create-game");

        endpoints.MapPost("/api/games/{publicCode}/join", async (string publicCode, HttpContext context,
            IAntiforgery antiforgery, IGameSessionManager sessions, IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!await IsAntiforgeryValidAsync(context, antiforgery))
                return Results.BadRequest(new { error = "AntiforgeryValidationFailed" });
            if (!PublicCodePattern().IsMatch(publicCode)) return Results.NotFound();
            var existing = context.Request.Cookies[PlayerSessionCookie.Name];
            var result = await sessions.JoinGameAsync(publicCode, existing, cancellationToken);
            if (result.Outcome is JoinOutcome.GameNotFound) return Results.NotFound();
            if (result.Outcome is JoinOutcome.RoomFull) return Results.Conflict(new { error = "RoomFull" });
            SetPlayerCookie(context, environment, result.PlayerToken!);
            return Results.Ok(new { publicCode = publicCode.ToUpperInvariant() });
        }).RequireRateLimiting("join-game");

        endpoints.MapPost("/api/games/{publicCode}/avatar", async (string publicCode, HttpContext context,
            IAntiforgery antiforgery, IAvatarImageProcessor processor, IAvatarStorage storage,
            IGameSessionManager sessions, GameSnapshotBroadcaster broadcaster, CancellationToken cancellationToken) =>
        {
            if (!await IsAntiforgeryValidAsync(context, antiforgery))
                return Results.BadRequest(new { error = "AntiforgeryValidationFailed" });
            if (!PublicCodePattern().IsMatch(publicCode)) return Results.NotFound();
            var token = context.Request.Cookies[PlayerSessionCookie.Name];
            if (token is null || !sessions.TryResolvePlayer(token, out _)) return Results.Unauthorized();
            if (!context.Request.HasFormContentType) return Results.BadRequest(new { error = "MultipartRequired" });
            try
            {
                var form = await context.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("avatar");
                if (file is null) return Results.BadRequest(new { error = "AvatarRequired" });
                await using var input = file.OpenReadStream();
                var processed = await processor.ProcessAsync(input, file.ContentType, file.Length, cancellationToken);
                var storageName = await storage.SaveAsync(processed.Content, cancellationToken);
                try
                {
                    var update = await sessions.SetAvatarAsync(publicCode, token, storageName,
                        processed.ContentType, cancellationToken);
                    if (update is null)
                    {
                        await storage.DeleteAsync(storageName, cancellationToken);
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    }
                    if (!update.Success)
                    {
                        await storage.DeleteAsync(storageName, cancellationToken);
                        return Results.Conflict(new { error = update.Error });
                    }
                    if (update.PreviousStorageName is not null)
                        await storage.DeleteAsync(update.PreviousStorageName, cancellationToken);
                    await broadcaster.BroadcastAsync(update.Snapshots, cancellationToken);
                    return Results.Ok(new { uploaded = true });
                }
                catch
                {
                    await storage.DeleteAsync(storageName, cancellationToken);
                    throw;
                }
            }
            catch (AvatarValidationException exception)
            {
                return Results.BadRequest(new { error = exception.Code });
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { error = "InvalidUpload" });
            }
        }).RequireRateLimiting("upload-avatar").WithMetadata(new RequestSizeLimitAttribute(6 * 1024 * 1024));

        endpoints.MapGet("/api/games/{publicCode}/players/{position:int}/avatar", async (
            string publicCode, int position, HttpContext context, IGameSessionManager sessions,
            IAvatarStorage storage, CancellationToken cancellationToken) =>
        {
            if (!PublicCodePattern().IsMatch(publicCode) || !Enum.IsDefined(typeof(PlayerPosition), position))
                return Results.NotFound();
            var token = context.Request.Cookies[PlayerSessionCookie.Name];
            if (token is null) return Results.Unauthorized();
            var avatar = await sessions.GetAvatarAsync(publicCode, token, (PlayerPosition)position, cancellationToken);
            if (avatar is null) return Results.NotFound();
            var stream = await storage.OpenReadAsync(avatar.StorageName, cancellationToken);
            if (stream is null) return Results.NotFound();
            context.Response.Headers.CacheControl = "private, no-store, max-age=0";
            context.Response.Headers.ContentDisposition = "inline";
            return Results.Stream(stream, avatar.ContentType, enableRangeProcessing: false);
        });

        endpoints.MapGet("/game/{publicCode}", (string publicCode, IGameSessionManager sessions,
            IWebHostEnvironment environment) =>
        {
            if (!PublicCodePattern().IsMatch(publicCode) || !sessions.GameExists(publicCode)) return Results.NotFound();
            return Results.File(Path.Combine(environment.WebRootPath, "index.html"), "text/html");
        }).RequireRateLimiting("join-game");

        return endpoints;
    }

    private static void SetPlayerCookie(HttpContext context, IWebHostEnvironment environment, string token) =>
        context.Response.Cookies.Append(PlayerSessionCookie.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment() && !environment.IsEnvironment("Testing"),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(8),
            IsEssential = true,
            Path = "/"
        });

    private static async Task<bool> IsAntiforgeryValidAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    [GeneratedRegex("^[2-9A-HJ-NP-Z]{8}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex PublicCodePattern();
}
