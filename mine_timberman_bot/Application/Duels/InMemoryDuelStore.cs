using System.Collections.Concurrent;

namespace MineTimbermanBot.Application.Duels;

public sealed class InMemoryDuelStore : IDuelStore
{
    private readonly ConcurrentDictionary<string, Duel> _duels = new(StringComparer.OrdinalIgnoreCase);

    public bool TryCreate(Duel duel)
    {
        if (FindByUser(duel.ChallengerUserId) is not null || FindByUser(duel.OpponentUserId) is not null)
        {
            return false;
        }

        return _duels.TryAdd(duel.Id, duel);
    }

    public Duel? Get(string duelId) => _duels.GetValueOrDefault(duelId);

    public Duel? FindByUser(long userId) => _duels.Values.FirstOrDefault(duel => duel.IsParticipant(userId));

    public IReadOnlyList<Duel> GetAll() => _duels.Values.ToList();

    public bool Remove(string duelId) => _duels.TryRemove(duelId, out _);
}
