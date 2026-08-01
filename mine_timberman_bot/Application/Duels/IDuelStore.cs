namespace MineTimbermanBot.Application.Duels;

public interface IDuelStore
{
    bool TryCreate(Duel duel);

    Duel? Get(string duelId);

    Duel? FindByUser(long userId);

    IReadOnlyList<Duel> GetAll();

    bool Remove(string duelId);
}
