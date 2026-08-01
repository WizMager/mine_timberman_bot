namespace MineTimbermanBot.Application.Sessions;

public interface IUserSessionStore
{
    UserSession GetOrCreate(long userId);

    bool TryGet(long userId, out UserSession session);

    void RegisterCharacterInChat(long chatId, long userId);

    bool IsCharacterInChat(long chatId, long userId);

    long? TryPickRandomOpponent(long chatId, long excludeUserId, Func<long, bool>? isBusy = null);
}
