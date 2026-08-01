using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Commands;
using MineTimbermanBot.Application.Duels;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineTimbermanBot.Features.Commands;

public sealed class FightCommand(
    IUserSessionStore sessionStore,
    IDuelStore duelStore,
    ILogger<FightCommand> logger) : IBotCommand
{
    public string Name => "fight";

    public string Description => "Вызвать случайного крепиля на сражение на \"шпагах\" на этом участке(в группе)";

    public async Task ExecuteAsync(BotCommandContext context, CancellationToken cancellationToken)
    {
        var chat = context.Message.Chat;
        if (chat.Type is not (ChatType.Group or ChatType.Supergroup))
        {
            await context.BotClient.SendMessage(
                chat,
                "Сам с собой будешь сражаться? Это работает только в группе",
                cancellationToken: cancellationToken);
            return;
        }

        if (context.Message.From is not { } user)
        {
            await context.BotClient.SendMessage(
                chat,
                "Не удалось определить пользователя.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            await context.BotClient.DeleteMessage(chat, context.Message.Id, cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not delete message {MessageId} in chat {ChatId}",
                context.Message.Id,
                chat.Id);
        }

        if (!sessionStore.TryGet(user.Id, out var challenger) || challenger.CharacterName is null || !sessionStore.IsCharacterInChat(chat.Id, user.Id))
        {
            await context.BotClient.SendMessage(
                chat,
                "Без крепиля на бой не ходят. Сначала /create в этой группе.",
                cancellationToken: cancellationToken);
            return;
        }

        if (duelStore.FindByUser(user.Id) is not null)
        {
            await context.BotClient.SendMessage(
                chat,
                "Ты уже в бою. Дождись конца дня или хода соперника.",
                cancellationToken: cancellationToken);
            return;
        }

        var opponentId = sessionStore.TryPickRandomOpponent(
            chat.Id,
            user.Id,
            busyUserId => duelStore.FindByUser(busyUserId) is not null);

        if (opponentId is null || !sessionStore.TryGet(opponentId.Value, out var opponent) || opponent.CharacterName is null)
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

        if (!duelStore.TryCreate(duel))
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
        }
        catch (ApiRequestException exception)
        {
            logger.LogInformation(
                exception,
                "Failed to start duel {DuelId}: DM delivery failed",
                duelId);

            duelStore.Remove(duelId);

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
