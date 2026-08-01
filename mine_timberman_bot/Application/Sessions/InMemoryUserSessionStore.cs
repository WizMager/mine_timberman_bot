using System.Collections.Concurrent;

namespace MineTimbermanBot.Application.Sessions;

public sealed class InMemoryUserSessionStore : IUserSessionStore
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<long, byte>> _charactersByChat = new();

    public UserSession GetOrCreate(long userId)
    {
        return _sessions.GetOrAdd(userId, static _ => new UserSession());
    }

    public bool TryGet(long userId, out UserSession session) => _sessions.TryGetValue(userId, out session!);

    public void RegisterCharacterInChat(long chatId, long userId) =>_charactersByChat.GetOrAdd(chatId, static _ => new ConcurrentDictionary<long, byte>()).TryAdd(userId, 0);

    public bool IsCharacterInChat(long chatId, long userId) => _charactersByChat.TryGetValue(chatId, out var members) && members.ContainsKey(userId);

    public long? TryPickRandomOpponent(long chatId, long excludeUserId, Func<long, bool>? isBusy = null)
    {
        if (!_charactersByChat.TryGetValue(chatId, out var members))
        {
            return null;
        }

        var candidates = members.Keys
            .Where(userId =>
                userId != excludeUserId
                && _sessions.TryGetValue(userId, out var session)
                && session.CharacterName is not null
                && (isBusy is null || !isBusy(userId)))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }
}
