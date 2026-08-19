using Abc.JogoDoVelho.Infrastructure.Persistence;

namespace Abc.JogoDoVelho.Tests;

public sealed class DatabaseConnectionStringTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequireRejectsMissingConfiguration(string? value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DatabaseConnectionString.Require(value));
        Assert.Contains("ConnectionStrings:Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RequireReturnsConfiguredValue()
    {
        const string value = "Host=database;Database=game";
        Assert.Equal(value, DatabaseConnectionString.Require(value));
    }
}

