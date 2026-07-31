using Telegram.Bot;
using Telegram.Bot.Types;

namespace MineTimbermanBot.Application.Commands;

public interface IBotCommand
{
    string Name { get; }

    string Description { get; }

    Task ExecuteAsync(BotCommandContext context, CancellationToken cancellationToken);
}

public sealed record BotCommandContext(
    ITelegramBotClient BotClient,
    Message Message,
    string Arguments);
