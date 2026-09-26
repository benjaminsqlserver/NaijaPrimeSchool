using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NaijaPrimeSchool.Infrastructure.Notifications;

// Polls for due notifications every DispatchIntervalSeconds and drains them
// batch by batch, each batch in its own DI scope.
public sealed class NotificationDispatchWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    ILogger<NotificationDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (!opts.Enabled)
        {
            logger.LogInformation("Announcement notifications are disabled (Notifications:Enabled = false).");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, opts.DispatchIntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                int processed;
                do
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<NotificationDispatcher>();
                    processed = await dispatcher.DispatchDueAsync(stoppingToken);
                }
                while (processed >= Math.Max(1, opts.BatchSize) && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Typically the database being briefly unreachable; try again next tick.
                logger.LogError(ex, "Announcement notification dispatch failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
