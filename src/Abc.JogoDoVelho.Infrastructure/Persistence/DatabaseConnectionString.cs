namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public static class DatabaseConnectionString
{
    public static string Require(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("The ConnectionStrings:Postgres configuration value is required.");
        }

        return value;
    }
}

