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

        var userId = context.Callback.From.Id;
        var duel = await duelStore.TrySetChoiceAsync(parts[0], userId, choice, auto: false, cancellationToken);
        if (duel is null)
        {
            var existing = await duelStore.GetAsync(parts[0], cancellationToken);
            if (existing is null)
            {
                return new CallbackHandleResult("Этот бой уже закончился.");
            }

            if (!existing.IsParticipant(userId))
            {
                return new CallbackHandleResult("Это не твой бой.");
            }

            return new CallbackHandleResult("Ты уже сходил.");
        }

        var ownDmMessageId = userId == duel.ChallengerUserId
            ? duel.ChallengerDmMessageId
            : duel.OpponentDmMessageId;

        await duelResolver.RemoveChoiceKeyboardAsync(userId, ownDmMessageId, cancellationToken);

        if (duel.BothChosen)
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