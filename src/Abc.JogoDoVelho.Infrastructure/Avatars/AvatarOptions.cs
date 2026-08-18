namespace Abc.JogoDoVelho.Infrastructure.Avatars;

public sealed class AvatarOptions
{
    public const string SectionName = "AvatarStorage";
    public string RootPath { get; set; } = "storage/avatars";
    public long MaximumUploadBytes { get; set; } = 5 * 1024 * 1024;
    public int MaximumDimension { get; set; } = 4096;
    public int OutputSize { get; set; } = 512;
    public int RetentionHours { get; set; } = 24;
    public int CleanupMinutes { get; set; } = 15;
}
