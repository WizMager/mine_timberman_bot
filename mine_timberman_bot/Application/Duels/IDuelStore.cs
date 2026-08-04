namespace MineTimbermanBot.Application.Duels;

public interface IDuelStore
{
    Task<bool> TryCreateAsync(Duel duel, CancellationToken cancellationToken = default);

    Task<Duel?> GetAsync(string duelId, CancellationToken cancellationToken = default);

    Task<Duel?> FindByUserAsync(long userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Duel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string duelId, CancellationToken cancellationToken = default);

    Task SaveAsync(Duel duel, CancellationToken cancellationToken = default);

    Task<Duel?> TrySetChoiceAsync(
        string duelId,
        long userId,
        FightChoice choice,
        bool auto = false,
        CancellationToken cancellationToken = default);
}
