using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application.Commands;

public abstract class BotCommandBase(ILogger logger, IUserSessionStore sessionStore) : IBotCommand
{
    protected IUserSessionStore SessionStore { get; } = sessionStore;

    public abstract string Name { get; }

    public abstract string Description { get; }

    protected virtual string MissingCharacterMessage => string.Empty;

    public async Task ExecuteAsync(BotCommandContext context, CancellationToken cancellationToken)
    {
        if (!await BeforeExecuteAsync(context, cancellationToken))
        {
            return;
        }

        await TryDeleteCommandMessageAsync(context, cancellationToken);

        if (context.Message.From is not { } user)
        {
            await context.BotClient.SendMessage(
                context.Message.Chat,
                "Не удалось определить пользователя для игровой сессии.",
                cancellationToken: cancellationToken);
            return;
        }

        if (MissingCharacterMessage.Length > 0)
        {
            var session = await SessionStore.TryGetAsync(user.Id, cancellationToken);
            if (session?.CharacterName is null)
            {
                await context.BotClient.SendMessage(
                    context.Message.Chat,
                    MissingCharacterMessage,
                    cancellationToken: cancellationToken);
                return;
            }
        }

        await ExecuteCoreAsync(context, user, cancellationToken);
    }

    protected virtual Task<bool> BeforeExecuteAsync(BotCommandContext context, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    protected abstract Task ExecuteCoreAsync(
        BotCommandContext context,
        User user,
        CancellationToken cancellationToken);

    protected async Task TryDeleteCommandMessageAsync(BotCommandContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.BotClient.DeleteMessage(
                context.Message.Chat,
                context.Message.Id,
                cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            logger.LogDebug(
                exception,
                "Could not delete /{CommandName} message {MessageId} in chat {ChatId}",
                Name,
                context.Message.Id,
                context.Message.Chat.Id);
        }
    }
}
