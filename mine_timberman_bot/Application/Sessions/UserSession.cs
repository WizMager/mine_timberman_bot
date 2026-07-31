namespace MineTimbermanBot.Application.Sessions;

public static class UserSessionStates
{
    public const string None = "None";
    public const string Choosing = "Choosing";
    public const string Playing = "Playing";
}

public sealed class UserSession
{
    public string State { get; set; } = UserSessionStates.None;

    public int Score { get; set; }

    public string? SelectedSide { get; set; }
    
    public string? CharacterName { get; set; }
    public int BoltsInWorkSession { get; set; }
    public int LogsInWorkSession { get; set; }
    public double Lucky { get; set; }
    public DateTime LastWorkTime { get; set; }
}
