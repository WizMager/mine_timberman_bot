namespace MineTimbermanBot.Telegram;

public sealed class BotIdentity
{
    public string Username { get; private set; } = string.Empty;

    public void SetUsername(string username)
    {
        Username = username.Trim().TrimStart('@');
    }

    public bool IsAddressedToThisBot(string? mentionedBotUsername)
    {
        if (string.IsNullOrWhiteSpace(mentionedBotUsername))
        {
            return true;
        }

        return string.Equals(
            mentionedBotUsername.Trim().TrimStart('@'),
            Username,
            StringComparison.OrdinalIgnoreCase);
    }
}
