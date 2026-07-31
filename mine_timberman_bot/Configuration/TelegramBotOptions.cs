namespace MineTimbermanBot.Configuration;

public sealed class TelegramBotOptions
{
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = string.Empty;

    public bool DropPendingUpdates { get; init; }
}
