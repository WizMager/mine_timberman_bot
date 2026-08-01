namespace MineTimbermanBot.Application.Duels;

public sealed class Duel
{
    public required string Id { get; init; }

    public long ChatId { get; init; }

    public int StatusMessageId { get; set; }

    public long ChallengerUserId { get; init; }

    public long OpponentUserId { get; init; }

    public string ChallengerName { get; init; } = string.Empty;

    public string OpponentName { get; init; } = string.Empty;

    public int? ChallengerDmMessageId { get; set; }

    public int? OpponentDmMessageId { get; set; }

    public FightChoice? ChallengerChoice { get; set; }

    public FightChoice? OpponentChoice { get; set; }

    public bool ChallengerChoiceAuto { get; set; }

    public bool OpponentChoiceAuto { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public object Sync { get; } = new();

    public bool IsParticipant(long userId) =>
        userId == ChallengerUserId || userId == OpponentUserId;

    public bool BothChosen => ChallengerChoice is not null && OpponentChoice is not null;

    public static string BuildStatusText(Duel duel)
    {
        var challengerStatus = duel.ChallengerChoice is null ? "ждёт" : "ход сделан";
        var opponentStatus = duel.OpponentChoice is null ? "ждёт" : "ход сделан";

        return
            $"""
            Бой до конца дня!
            {duel.ChallengerName}: {challengerStatus}
            {duel.OpponentName}: {opponentStatus}
            Выбор — в личке с ботом.
            """;
    }
}
