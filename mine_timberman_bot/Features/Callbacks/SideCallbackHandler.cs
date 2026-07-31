using MineTimbermanBot.Application.Callbacks;
using MineTimbermanBot.Application.Sessions;

namespace MineTimbermanBot.Features.Callbacks;

public sealed class SideCallbackHandler : ICallbackHandler
{
    public string Prefix => "side";

    public Task<CallbackHandleResult> HandleAsync(
        BotCallbackContext context,
        CancellationToken cancellationToken)
    {
        CallbackHandleResult result;

        lock (context.Session)
        {
            if (context.Session.State != UserSessionStates.Choosing)
            {
                result = new CallbackHandleResult(
                    "Это меню уже неактуально. Отправьте /play, чтобы начать заново.");
            }
            else
            {
                result = context.Payload switch
                {
                    "left" => ApplySide(context.Session, "left", "Вы выбрали левую сторону. Играем слева!"),
                    "right" => ApplySide(context.Session, "right", "Вы выбрали правую сторону. Играем справа!"),
                    _ => new CallbackHandleResult(
                        "Эта кнопка больше не поддерживается. Отправьте /play и выберите снова.")
                };
            }
        }

        return Task.FromResult(result);
    }

    private static CallbackHandleResult ApplySide(
        UserSession session,
        string side,
        string responseText)
    {
        session.SelectedSide = side;
        session.State = UserSessionStates.Playing;
        return new CallbackHandleResult(responseText);
    }
}
