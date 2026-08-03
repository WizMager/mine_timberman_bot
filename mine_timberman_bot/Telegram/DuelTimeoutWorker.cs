using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Duels;

namespace MineTimbermanBot.Telegram;

public sealed class DuelTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DuelTimeoutWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredDuelsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Duel timeout tick failed");
            }

            try
            {
                await Task.Delay(TickInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessExpiredDuelsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var duelStore = scope.ServiceProvider.GetRequiredService<IDuelStore>();
        var duelResolver = scope.ServiceProvider.GetRequiredService<DuelResolver>();

        var today = DateTime.Today;
        var expiredIds = (await duelStore.GetAllAsync(cancellationToken))
            .Where(duel => duel.CreatedAt.Date < today)
            .Select(duel => duel.Id)
            .ToList();

        foreach (var duelId in expiredIds)
        {
            var duel = await duelStore.GetAsync(duelId, cancellationToken);
            if (duel is null)
            {
                continue;
            }

            if (duel.ChallengerChoice is null)
            {
                await duelStore.TrySetChoiceAsync(
                    duelId,
                    duel.ChallengerUserId,
                    RpsChoiceExtensions.RandomChoice(),
                    auto: true,
                    cancellationToken);
            }

            if (duel.OpponentChoice is null)
            {
                await duelStore.TrySetChoiceAsync(
                    duelId,
                    duel.OpponentUserId,
                    RpsChoiceExtensions.RandomChoice(),
                    auto: true,
                    cancellationToken);
            }

            duel = await duelStore.GetAsync(duelId, cancellationToken);
            if (duel is { BothChosen: true })
            {
                await duelResolver.ResolveAsync(duel, cancellationToken);
            }
        }
    }
}
