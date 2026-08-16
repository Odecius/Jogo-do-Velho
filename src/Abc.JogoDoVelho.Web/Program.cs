using Abc.JogoDoVelho.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseConnectionString.Require(builder.Configuration.GetConnectionString("Postgres"));

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

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
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = item => item.Tags.Contains("live") });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = item => item.Tags.Contains("ready") });

app.Run();

public partial class Program;
