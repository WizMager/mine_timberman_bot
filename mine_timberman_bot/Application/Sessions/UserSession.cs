namespace MineTimbermanBot.Application.Sessions;

public sealed class UserSession
{
    public long UserId { get; set; }

    public string? CharacterName { get; set; }

    public int BoltsInWorkSession { get; set; }

    public int LogsInWorkSession { get; set; }

    public int Force { get; set; }
    
    public int Lucky { get; set; }

    public DateTime LastRestTime { get; set; }
}
