using System.Collections.Concurrent;

namespace MineTimbermanBot.Application.Sessions;

public sealed class InMemoryUserSessionStore : IUserSessionStore
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public UserSession GetOrCreate(long userId)
    {
        return _sessions.GetOrAdd(userId, static _ => new UserSession());
    }
}
