using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Sessions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application.Callbacks;

public sealed class CallbackDispatcher
{
    private readonly IReadOnlyDictionary<string, ICallbackHandler> _handlers;
    private readonly IUserSessionStore _sessionStore;
    private readonly ILogger<CallbackDispatcher> _logger;

    public CallbackDispatcher(
        IEnumerable<ICallbackHandler> handlers,
        IUserSessionStore sessionStore,
        ILogger<CallbackDispatcher> logger
    )
    {
        _handlers = handlers.ToDictionary(handler => NormalizePrefix(handler.Prefix), StringComparer.OrdinalIgnoreCase);
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task DispatchAsync(ITelegramBotClient botClient, CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (!TryParseCallbackData(callback.Data, out var prefix, out var payload) || !_handlers.TryGetValue(prefix, out var handler))
        {
            if (prefix.Length > 0 && !_handlers.ContainsKey(prefix))
            {
                _logger.LogInformation(
                    "Unknown callback prefix {CallbackPrefix} from user {UserId}",
                    prefix,
                    callback.From.Id);
            }

            await botClient.AnswerCallbackQuery(
                callback.Id,
                "Эта кнопка больше не поддерживается.",
                cancellationToken: cancellationToken);

            await TryDeletePanelAsync(botClient, callback, cancellationToken);
            return;
        }

        var session = await _sessionStore.GetOrCreateAsync(callback.From.Id, cancellationToken);
        var context = new BotCallbackContext(botClient, callback, session, callback.Data!, payload);

        var result = await handler.HandleAsync(context, cancellationToken);

        _logger.LogInformation(
            "Callback {CallbackData} handled by {Handler} for user {UserId} in state {State}",
            callback.Data,
            handler.GetType().Name,
            callback.From.Id,
            session.State);

        await botClient.AnswerCallbackQuery(
            callback.Id,
            result.ToastText,
            cancellationToken: cancellationToken);
    }

    private async Task TryDeletePanelAsync(ITelegramBotClient botClient, CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (callback.Message is not { } panelMessage)
        {
            return;
        }

        try
        {
            await botClient.DeleteMessage(
                panelMessage.Chat,
                panelMessage.Id,
                cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            _logger.LogDebug(
                exception,
                "Could not delete callback panel {MessageId} in chat {ChatId}",
                panelMessage.Id,
                panelMessage.Chat.Id);
        }
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalizedPrefix = prefix.Trim().TrimEnd(':');

        if (normalizedPrefix.Length == 0 || normalizedPrefix.Contains(':'))
        {
            throw new InvalidOperationException(
                "Callback handler prefix must be a non-empty value without ':'.");
        }

        return normalizedPrefix;
    }

    private static bool TryParseCallbackData(string? data, out string prefix, out string payload)
    {
        prefix = string.Empty;
        payload = string.Empty;

        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        var separatorIndex = data.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == data.Length - 1)
        {
            return false;
        }

        prefix = data[..separatorIndex];
        payload = data[(separatorIndex + 1)..];

        return prefix.Length > 0;
    }
}
