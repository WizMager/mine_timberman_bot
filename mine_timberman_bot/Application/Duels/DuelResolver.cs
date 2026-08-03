using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace MineTimbermanBot.Application.Duels;

public sealed class DuelResolver(
    ITelegramBotClient botClient,
    IUserSessionStore sessionStore,
    IDuelStore duelStore,
    IUnitOfWork unitOfWork,
    ILogger<DuelResolver> logger)
{
    public async Task ResolveAsync(Duel duel, CancellationToken cancellationToken)
    {
        if (duel.ChallengerChoice is not { } challengerChoice || duel.OpponentChoice is not { } opponentChoice)
        {
            throw new InvalidOperationException("Cannot resolve duel without both choices.");
        }

        if (!await duelStore.RemoveAsync(duel.Id, cancellationToken))
        {
            return;
        }

        await CleanupMessagesAsync(duel, cancellationToken);

        var comparison = RpsChoiceExtensions.Compare(challengerChoice, opponentChoice);
        var autoNote = BuildAutoNote(duel);

        if (comparison == 0)
        {
            await botClient.SendMessage(
                duel.ChatId,
                $"""
                Ничья!
                {duel.ChallengerName}: {challengerChoice.ToRussian()}
                {duel.OpponentName}: {opponentChoice.ToRussian()}
                {autoNote}
                """.Trim(),
                cancellationToken: cancellationToken);
            return;
        }

        var winnerId = comparison > 0 ? duel.ChallengerUserId : duel.OpponentUserId;
        var loserId = comparison > 0 ? duel.OpponentUserId : duel.ChallengerUserId;
        var winnerName = comparison > 0 ? duel.ChallengerName : duel.OpponentName;
        var loserName = comparison > 0 ? duel.OpponentName : duel.ChallengerName;

        var winner = await sessionStore.GetOrCreateAsync(winnerId, cancellationToken);
        var loser = await sessionStore.GetOrCreateAsync(loserId, cancellationToken);

        winner.Force += 5;
        var stolenBolts = StealAmount(loser.BoltsInWorkSession);
        loser.BoltsInWorkSession -= stolenBolts;
        winner.BoltsInWorkSession += stolenBolts;

        var stolenLogs = 0;
        if (PassesForceCheck(winner.Force) && loser.LogsInWorkSession > 0)
        {
            stolenLogs = StealAmount(loser.LogsInWorkSession);
            loser.LogsInWorkSession -= stolenLogs;
            winner.LogsInWorkSession += stolenLogs;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var lootLines = new List<string>
        {
            $"{winnerName} побеждает!",
            $"{duel.ChallengerName}: {challengerChoice.ToRussian()}",
            $"{duel.OpponentName}: {opponentChoice.ToRussian()}",
            $"+5 силы (теперь {winner.Force}%)"
        };

        if (stolenBolts > 0)
        {
            lootLines.Add($"Утянул у {loserName} болтов: {stolenBolts}");
        }

        if (stolenLogs > 0)
        {
            lootLines.Add($"И ещё стоек: {stolenLogs}");
        }

        if (!string.IsNullOrWhiteSpace(autoNote))
        {
            lootLines.Add(autoNote);
        }

        await botClient.SendMessage(
            duel.ChatId,
            string.Join('\n', lootLines),
            cancellationToken: cancellationToken);
    }

    public async Task CancelAsync(Duel duel, string reason, CancellationToken cancellationToken)
    {
        if (!await duelStore.RemoveAsync(duel.Id, cancellationToken))
        {
            return;
        }

        await CleanupMessagesAsync(duel, cancellationToken);

        await botClient.SendMessage(
            duel.ChatId,
            reason,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateStatusAsync(Duel duel, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.EditMessageText(
                duel.ChatId,
                duel.StatusMessageId,
                Duel.BuildStatusText(duel),
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not edit duel status {MessageId} in chat {ChatId}",
                duel.StatusMessageId,
                duel.ChatId);
        }
    }

    public async Task RemoveChoiceKeyboardAsync(
        long userId,
        int? messageId,
        CancellationToken cancellationToken)
    {
        if (messageId is not { } id)
        {
            return;
        }

        try
        {
            await botClient.EditMessageReplyMarkup(
                userId,
                id,
                replyMarkup: null,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not clear duel keyboard for user {UserId} message {MessageId}",
                userId,
                id);
        }
    }

    private async Task CleanupMessagesAsync(Duel duel, CancellationToken cancellationToken)
    {
        await TryDeleteAsync(duel.ChatId, duel.StatusMessageId, cancellationToken);
        await TryDeleteAsync(duel.ChallengerUserId, duel.ChallengerDmMessageId, cancellationToken);
        await TryDeleteAsync(duel.OpponentUserId, duel.OpponentDmMessageId, cancellationToken);
    }

    private async Task TryDeleteAsync(long chatId, int? messageId, CancellationToken cancellationToken)
    {
        if (messageId is not { } id)
        {
            return;
        }

        try
        {
            await botClient.DeleteMessage(chatId, id, cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not delete message {MessageId} in chat {ChatId}",
                id,
                chatId);
        }
    }

    private static int StealAmount(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        return Math.Min(amount, Math.Max(1, amount / 4));
    }

    private static bool PassesForceCheck(int force)
    {
        var roll = force == 100
            ? Random.Shared.Next(101)
            : Random.Shared.Next(100);
        return roll < force;
    }

    private static string BuildAutoNote(Duel duel)
    {
        if (duel.ChallengerChoiceAuto && duel.OpponentChoiceAuto)
        {
            return "Ходы обоих выбраны автоматически.";
        }

        if (duel.ChallengerChoiceAuto)
        {
            return $"За {duel.ChallengerName} ход выбран автоматически.";
        }

        if (duel.OpponentChoiceAuto)
        {
            return $"За {duel.OpponentName} ход выбран автоматически.";
        }

        return string.Empty;
    }
}
