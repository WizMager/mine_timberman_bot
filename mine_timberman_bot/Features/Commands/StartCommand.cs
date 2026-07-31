using MineTimbermanBot.Application.Commands;
using Telegram.Bot;

namespace MineTimbermanBot.Features.Commands;

public sealed class StartCommand : IBotCommand
{
    public string Name => "start";

    public string Description => "Начать работу с ботом";

    public async Task ExecuteAsync(
        BotCommandContext context,
        CancellationToken cancellationToken)
    {
        var firstName = context.Message.From?.FirstName;
        var greeting = string.IsNullOrWhiteSpace(firstName)
            ? "Привет!"
            : $"Привет, {firstName}!";

        await context.BotClient.SendMessage(
            context.Message.Chat,
            $"{greeting}\n\nЯ — стартовый каркас Telegram-бота. Используйте /help.",
            cancellationToken: cancellationToken);
    }
}
