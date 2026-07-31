namespace MineTimbermanBot.Application.Sessions;

public static class UserSessionStates
{
    public const string None = "None";
    public const string ChoosingSide = "ChoosingSide";
    public const string Playing = "Playing";
}

public sealed class UserSession
{
    public string State { get; set; } = UserSessionStates.None;

    public int Score { get; set; }

    public string? SelectedSide { get; set; }
}
