using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application.Callbacks;

public interface ICallbackHandler
{
    string Prefix { get; }

    Task<CallbackHandleResult> HandleAsync(BotCallbackContext context,CancellationToken cancellationToken);
}

public sealed record BotCallbackContext(
    ITelegramBotClient BotClient,
    CallbackQuery Callback,
    UserSession Session,
    string Data,
    string Payload);

public sealed record CallbackHandleResult(string? ToastText = null);
