namespace MineTimbermanBot.Application.Sessions;

public interface IUserSessionStore
{
    Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default);

    Task<UserSession?> TryGetAsync(long userId, CancellationToken cancellationToken = default);

    Task RegisterCharacterInChatAsync(long chatId, long userId, CancellationToken cancellationToken = default);

    Task<bool> IsCharacterInChatAsync(long chatId, long userId, CancellationToken cancellationToken = default);

    Task<long?> TryPickRandomOpponentAsync( long chatId, long excludeUserId, Func<long, CancellationToken, Task<bool>>? isBusy = null, CancellationToken cancellationToken = default);
}
