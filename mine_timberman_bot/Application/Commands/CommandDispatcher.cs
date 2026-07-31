using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application.Commands;

public sealed class CommandDispatcher
{
    private static readonly char[] CommandSeparators = [' ', '\t', '\r', '\n'];

    private readonly IReadOnlyDictionary<string, IBotCommand> _commands;
    private readonly ILogger<CommandDispatcher> _logger;

    public CommandDispatcher(
        IEnumerable<IBotCommand> commands,
        ILogger<CommandDispatcher> logger)
    {
        _commands = commands.ToDictionary(
            command => NormalizeName(command.Name),
            StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task DispatchAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return;
        }

        if (!TryParseCommand(message.Text, out var commandName, out var arguments))
        {
            await botClient.SendMessage(
                message.Chat,
                "Я обрабатываю команды. Отправьте /help, чтобы увидеть список.",
                cancellationToken: cancellationToken);
            return;
        }

        if (!_commands.TryGetValue(commandName, out var command))
        {
            _logger.LogInformation(
                "Unknown command /{CommandName} received from chat {ChatId}",
                commandName,
                message.Chat.Id);

            await botClient.SendMessage(
                message.Chat,
                $"Неизвестная команда /{commandName}. Используйте /help.",
                cancellationToken: cancellationToken);
            return;
        }

        var context = new BotCommandContext(botClient, message, arguments);
        await command.ExecuteAsync(context, cancellationToken);
    }

    private static string NormalizeName(string name)
    {
        var normalizedName = name.Trim().TrimStart('/');

        if (normalizedName.Length == 0)
        {
            throw new InvalidOperationException("Bot command name cannot be empty.");
        }

        return normalizedName;
    }

    private static bool TryParseCommand(
        string text,
        out string commandName,
        out string arguments)
    {
        commandName = string.Empty;
        arguments = string.Empty;

        if (text.Length < 2 || text[0] != '/')
        {
            return false;
        }

        var separatorIndex = text.IndexOfAny(CommandSeparators);
        var commandToken = separatorIndex < 0
            ? text[1..]
            : text[1..separatorIndex];

        var mentionIndex = commandToken.IndexOf('@');
        if (mentionIndex >= 0)
        {
            commandToken = commandToken[..mentionIndex];
        }

        if (commandToken.Length == 0)
        {
            return false;
        }

        commandName = commandToken;
        arguments = separatorIndex < 0
            ? string.Empty
            : text[(separatorIndex + 1)..].Trim();

        return true;
    }
}
