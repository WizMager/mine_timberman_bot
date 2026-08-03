using Microsoft.EntityFrameworkCore;
using MineTimbermanBot.Application.Duels;

namespace MineTimbermanBot.Infrastructure.Persistence;

public sealed class EfDuelStore(AppDbContext db) : IDuelStore
{
    public async Task<bool> TryCreateAsync(Duel duel, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var busy = await db.Duels.AnyAsync(
            existing =>
                existing.ChallengerUserId == duel.ChallengerUserId
                || existing.OpponentUserId == duel.ChallengerUserId
                || existing.ChallengerUserId == duel.OpponentUserId
                || existing.OpponentUserId == duel.OpponentUserId,
            cancellationToken);

        if (busy)
        {
            return false;
        }

        db.Duels.Add(duel);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<Duel?> GetAsync(string duelId, CancellationToken cancellationToken = default) =>
        db.Duels.FirstOrDefaultAsync(duel => duel.Id == duelId, cancellationToken);

    public Task<Duel?> FindByUserAsync(long userId, CancellationToken cancellationToken = default) =>
        db.Duels.FirstOrDefaultAsync(
            duel => duel.ChallengerUserId == userId || duel.OpponentUserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Duel>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Duels.ToListAsync(cancellationToken);

    public async Task<bool> RemoveAsync(string duelId, CancellationToken cancellationToken = default)
    {
        var duel = await db.Duels.FirstOrDefaultAsync(existing => existing.Id == duelId, cancellationToken);
        if (duel is null)
        {
            return false;
        }

        db.Duels.Remove(duel);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Duel?> TrySetChoiceAsync(
        string duelId,
        long userId,
        FightChoice choice,
        bool auto = false,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var duel = await db.Duels.FirstOrDefaultAsync(existing => existing.Id == duelId, cancellationToken);
        if (duel is null || !duel.IsParticipant(userId))
        {
            return null;
        }

        if (userId == duel.ChallengerUserId)
        {
            if (duel.ChallengerChoice is not null)
            {
                return null;
            }

            duel.ChallengerChoice = choice;
            duel.ChallengerChoiceAuto = auto;
        }
        else
        {
            if (duel.OpponentChoice is not null)
            {
                return null;
            }

            duel.OpponentChoice = choice;
            duel.OpponentChoiceAuto = auto;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return duel;
    }
}
