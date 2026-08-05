using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Duels;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineTimbermanBot.Features.Commands;

public sealed class FightCommand(
    IUserSessionStore sessionStore,
    IDuelStore duelStore,
    ILogger<FightCommand> logger
) : BotCommandBase(logger, sessionStore)
{
    public override string Name => "fight";

    public override string Description => "Вызвать случайного крепиля на сражение на \"шпагах\" на этом участке(в группе)";

    protected override async Task<bool> BeforeExecuteAsync(BotCommandContext context, CancellationToken cancellationToken)
    {
        var chat = context.Message.Chat;
        if (chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            return true;
        }

        await context.BotClient.SendMessage(
            chat,
            "Сам с собой будешь сражаться? Это работает только в группе",
            cancellationToken: cancellationToken);
        return false;
    }

    protected override async Task ExecuteCoreAsync(BotCommandContext context, User user, CancellationToken cancellationToken)
    {
        var chat = context.Message.Chat;

        var challenger = await SessionStore.TryGetAsync(user.Id, cancellationToken);
        if (challenger?.CharacterName is null)
        {
            await context.BotClient.SendMessage(
                chat,
                "Без крепиля на бой не ходят. Сначала /create.",
                cancellationToken: cancellationToken);
            return;
        }

        if (!await SessionStore.IsCharacterInChatAsync(chat.Id, user.Id, cancellationToken))
        {
            await SessionStore.RegisterCharacterInChatAsync(chat.Id, user.Id, cancellationToken);
        }
        
        if (await duelStore.FindByUserAsync(user.Id, cancellationToken) is not null)
        {
            await context.BotClient.SendMessage(
                chat,
                "Ты уже в бою. Дождись конца дня или хода соперника.",
                cancellationToken: cancellationToken);
            return;
        }
        
        if (challenger.Force < 15)
        {
            await context.BotClient.SendMessage(
                chat,
                $"У твоего {challenger.CharacterName} нету сил поднять бурилку, поднять стойку или держать бензопилу",
                cancellationToken: cancellationToken);
            return;
        }

        var opponentId = await SessionStore.TryPickRandomOpponentAsync(
            chat.Id,
            user.Id,
            async (busyUserId, ct) => await duelStore.FindByUserAsync(busyUserId, ct) is not null,
            cancellationToken);

        if (opponentId is null)
        {
            await context.BotClient.SendMessage(
                chat,
                "Некого звать: в этой группе нет других крепилей (или все уже в скрестили болты).",
                cancellationToken: cancellationToken);
            return;
        }

        var opponent = await SessionStore.TryGetAsync(opponentId.Value, cancellationToken);
        if (opponent?.CharacterName is null)
        {
            await context.BotClient.SendMessage(
                chat,
                "Некого звать: в этой группе нет других крепилей (или все уже в скрестили болты).",
                cancellationToken: cancellationToken);
            return;
        }

        var duelId = Guid.NewGuid().ToString("N")[..8];
        var duel = new Duel
        {
            Id = duelId,
            ChatId = chat.Id,
            ChallengerUserId = user.Id,
            OpponentUserId = opponentId.Value,
            ChallengerName = challenger.CharacterName,
            OpponentName = opponent.CharacterName,
            CreatedAt = DateTime.Now
        };

        if (!await duelStore.TryCreateAsync(duel, cancellationToken))
        {
            await context.BotClient.SendMessage(
                chat,
                "Не удалось начать бой — кто-то уже занят.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var statusMessage = await context.BotClient.SendMessage(
                chat,
                Duel.BuildStatusText(duel),
                cancellationToken: cancellationToken);
            duel.StatusMessageId = statusMessage.Id;

            var keyboard = BuildChoiceKeyboard(duelId);

            var challengerDm = await context.BotClient.SendMessage(
                user.Id,
                $"Бой с {duel.OpponentName}. Выбери ход — соперник его не увидит:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            duel.ChallengerDmMessageId = challengerDm.Id;

            var opponentDm = await context.BotClient.SendMessage(
                opponentId.Value,
                $"Тебя вызвал {duel.ChallengerName}. Выбери ход — соперник его не увидит:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            duel.OpponentDmMessageId = opponentDm.Id;

            await duelStore.SaveAsync(duel, cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogInformation(
                exception,
                "Failed to start duel {DuelId}: DM delivery failed",
                duelId);

            await duelStore.RemoveAsync(duelId, cancellationToken);

            if (duel.StatusMessageId != 0)
            {
                try
                {
                    await context.BotClient.DeleteMessage(
                        chat,
                        duel.StatusMessageId,
                        cancellationToken);
                }
                catch (ApiRequestException deleteException)
                {
                    logger.LogDebug(deleteException, "Could not delete failed duel status");
                }
            }

            if (duel.ChallengerDmMessageId is { } challengerDmId)
            {
                try
                {
                    await context.BotClient.DeleteMessage(user.Id, challengerDmId, cancellationToken);
                }
                catch (ApiRequestException)
                {
                    // ignore
                }
            }

            await context.BotClient.SendMessage(
                chat,
                "Не смог открыть личку одному из бойцов. Оба должны нажать /start у бота в ЛС.",
                cancellationToken: cancellationToken);
        }
    }

    private static InlineKeyboardMarkup BuildChoiceKeyboard(string duelId) =>
        new(
        [
            [
                InlineKeyboardButton.WithCallbackData("Бурилка(Камень)", $"fight:{duelId}:rock"),
                InlineKeyboardButton.WithCallbackData("Бензопила(Ножницы)", $"fight:{duelId}:scissors"),
                InlineKeyboardButton.WithCallbackData("РудСтойка(Бумага)", $"fight:{duelId}:paper")
            ]
        ]);
}
