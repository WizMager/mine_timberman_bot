using MineTimbermanBot.Application.Callbacks;
using MineTimbermanBot.Application.Duels;

namespace MineTimbermanBot.Features.Callbacks;

public sealed class FightCallbackHandler(IDuelStore duelStore, DuelResolver duelResolver) : ICallbackHandler
{
    public string Prefix => "fight";

    public async Task<CallbackHandleResult> HandleAsync(BotCallbackContext context, CancellationToken cancellationToken)
    {
        var parts = context.Payload.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || !RpsChoiceExtensions.TryParse(parts[1], out var choice))
        {
            return new CallbackHandleResult("Кнопка устарела. Жди новый бой.");
        }

        var duel = duelStore.Get(parts[0]);
        if (duel is null)
        {
            return new CallbackHandleResult("Этот бой уже закончился.");
        }

        var userId = context.Callback.From.Id;
        if (!duel.IsParticipant(userId))
        {
            return new CallbackHandleResult("Это не твой бой.");
        }

        bool shouldResolve;
        int? ownDmMessageId;

        lock (duel.Sync)
        {
            if (userId == duel.ChallengerUserId)
            {
                if (duel.ChallengerChoice is not null)
                {
                    return new CallbackHandleResult("Ты уже сходил.");
                }

                duel.ChallengerChoice = choice;
                ownDmMessageId = duel.ChallengerDmMessageId;
            }
            else
            {
                if (duel.OpponentChoice is not null)
                {
                    return new CallbackHandleResult("Ты уже сходил.");
                }

                duel.OpponentChoice = choice;
                ownDmMessageId = duel.OpponentDmMessageId;
            }

            shouldResolve = duel.BothChosen;
        }

        await duelResolver.RemoveChoiceKeyboardAsync(userId, ownDmMessageId, cancellationToken);

        if (shouldResolve)
        {
            await duelResolver.ResolveAsync(duel, cancellationToken);
        }
        else
        {
            await duelResolver.UpdateStatusAsync(duel, cancellationToken);
        }

        return new CallbackHandleResult("Принято.");
    }
}
