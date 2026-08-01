using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MineTimbermanBot.Application.Duels;

namespace MineTimbermanBot.Telegram;

public sealed class DuelTimeoutWorker(
    IDuelStore duelStore,
    DuelResolver duelResolver,
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
        var today = DateTime.Today;
        foreach (var duel in duelStore.GetAll())
        {
            if (duel.CreatedAt.Date >= today)
            {
                continue;
            }

            lock (duel.Sync)
            {
                if (duel.ChallengerChoice is null)
                {
                    duel.ChallengerChoice = RpsChoiceExtensions.RandomChoice();
                    duel.ChallengerChoiceAuto = true;
                }

                if (duel.OpponentChoice is null)
                {
                    duel.OpponentChoice = RpsChoiceExtensions.RandomChoice();
                    duel.OpponentChoiceAuto = true;
                }
            }

            await duelResolver.ResolveAsync(duel, cancellationToken);
        }
    }
}
