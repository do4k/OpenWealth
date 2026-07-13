namespace OpenWealth.Api.Services;

/// <summary>
/// Background loop that applies due payday accruals shortly after startup and
/// then every few hours, so paydays are caught the day they happen and missed
/// ones are back-filled after downtime.
/// </summary>
public class AccrualWorker(IServiceScopeFactory scopeFactory, ILogger<AccrualWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to finish migrations/startup work.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var accruals = scope.ServiceProvider.GetRequiredService<AccrualService>();
                var applied = await accruals.RunForAllDueUsersAsync(
                    DateOnly.FromDateTime(DateTime.UtcNow), stoppingToken);
                if (applied > 0)
                    logger.LogInformation("Payday run complete: {Count} accrual(s) applied", applied);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payday accrual run failed; will retry next cycle");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
