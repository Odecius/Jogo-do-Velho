using System.Text.RegularExpressions;
using Abc.JogoDoVelho.Web.Multiplayer;
using Microsoft.AspNetCore.Antiforgery;

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
