namespace Abc.JogoDoVelho.Web.Multiplayer;

public sealed class GameSessionOptions
{
    public const string SectionName = "GameSessions";
    public int InactivityHours { get; set; } = 24;
    public int CleanupMinutes { get; set; } = 15;
}
