using Microsoft.EntityFrameworkCore;
using MineTimbermanBot.Application.Sessions;
using MineTimbermanBot.Infrastructure.Persistence.Entities;

namespace MineTimbermanBot.Infrastructure.Persistence;

public sealed class EfUserSessionStore(AppDbContext db) : IUserSessionStore
{
    public async Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var session = await db.Characters.FindAsync([userId], cancellationToken);
        if (session is not null)
        {
            return session;
        }

        session = new UserSession { UserId = userId };
        db.Characters.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public Task<UserSession?> TryGetAsync(long userId, CancellationToken cancellationToken = default) => db.Characters.FindAsync([userId], cancellationToken).AsTask();

    public async Task RegisterCharacterInChatAsync(long chatId, long userId, CancellationToken cancellationToken = default)
    {
        var exists = await db.ChatMemberships.AnyAsync(membership => membership.ChatId == chatId && membership.UserId == userId, cancellationToken);

        if (exists)
        {
            return;
        }

        db.ChatMemberships.Add(new ChatMembership { ChatId = chatId, UserId = userId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsCharacterInChatAsync(long chatId, long userId, CancellationToken cancellationToken = default) =>
        db.ChatMemberships.AnyAsync(membership => membership.ChatId == chatId && membership.UserId == userId, cancellationToken);

    public async Task<long?> TryPickRandomOpponentAsync(
        long chatId,
        long excludeUserId,
        Func<long, CancellationToken, Task<bool>>? isBusy = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = await db.ChatMemberships
            .AsNoTracking()
            .Where(membership => membership.ChatId == chatId && membership.UserId != excludeUserId)
            .Join(
                db.Characters.AsNoTracking(),
                membership => membership.UserId,
                character => character.UserId,
                (_, character) => character)
            .Where(character => character.CharacterName != null)
            .Select(character => character.UserId)
            .ToListAsync(cancellationToken);

        if (isBusy is not null)
        {
            var free = new List<long>(candidates.Count);
            foreach (var candidateId in candidates)
            {
                if (!await isBusy(candidateId, cancellationToken))
                {
                    free.Add(candidateId);
                }
            }

            candidates = free;
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }
}