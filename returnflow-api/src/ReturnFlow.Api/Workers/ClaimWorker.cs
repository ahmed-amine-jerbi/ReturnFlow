using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Workers;

/// <summary>Background worker: picks up new claims so that filing a claim never waits for the AI.</summary>
public partial class ClaimWorker(ClaimQueue queue, IServiceScopeFactory scopes, ILogger<ClaimWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pick up claims that were still waiting when the app last stopped.
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.Claims
                .Where(c => c.Processing == ProcessingState.Analyzing)
                .Select(c => c.Id)
                .ToListAsync(stoppingToken);
            foreach (var id in pending) queue.Enqueue(id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogRecoveryFailed(log, ex);
        }

        try
        {
            await foreach (var claimId in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ClaimProcessor>();
                    await processor.ProcessAsync(claimId, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogProcessingFailed(log, ex, claimId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not recover pending claims at startup")]
    private static partial void LogRecoveryFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process claim {ClaimId}")]
    private static partial void LogProcessingFailed(ILogger logger, Exception exception, int claimId);
}
