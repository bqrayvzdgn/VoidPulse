using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoidPulse.Domain.Interfaces;

namespace VoidPulse.Infrastructure.Services;

public class RetentionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionCleanupService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public RetentionCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredFlowsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during retention cleanup");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredFlowsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var retentionRepo = scope.ServiceProvider.GetRequiredService<IRetentionPolicyRepository>();
        var trafficRepo = scope.ServiceProvider.GetRequiredService<ITrafficFlowRepository>();

        var policies = await retentionRepo.GetAllAsync();

        foreach (var policy in policies)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var cutoff = DateTime.UtcNow.AddDays(-policy.RetentionDays);
            var deleted = await trafficRepo.DeleteOlderThanAsync(policy.TenantId, cutoff);

            if (deleted > 0)
            {
                _logger.LogInformation(
                    "Retention cleanup: deleted {Count} expired flows for tenant {TenantId} (cutoff: {Cutoff})",
                    deleted, policy.TenantId, cutoff);
            }
        }
    }
}
