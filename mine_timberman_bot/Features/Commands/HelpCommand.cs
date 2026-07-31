using MineTimbermanBot.Application.Commands;
using Telegram.Bot;

namespace MineTimbermanBot.Features.Commands;

public sealed class HelpCommand : IBotCommand
{
    public string Name => "help";

    public string Description => "Показать список команд";

    public async Task ExecuteAsync(
        BotCommandContext context,
        CancellationToken cancellationToken)
    {
        const string helpText =
            """
            Доступные команды:
            /start — начать работу с ботом
            /help — показать эту справку
            /echo <текст> — повторить переданный текст
            /play — выбрать сторону с помощью inline-кнопок
            /create - создать крепиля
            /work - работнуть
            /rest - попытаться кемарнуть до приезда ИТР
            """;

        await context.BotClient.SendMessage(
            context.Message.Chat,
            helpText,
            cancellationToken: cancellationToken);
    }
}
