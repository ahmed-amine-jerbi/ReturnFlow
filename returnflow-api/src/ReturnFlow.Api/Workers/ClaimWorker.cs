using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Workers;

/// <summary>Background worker: picks up new claims so that filing a claim never waits for the AI.</summary>
public class ClaimWorker(ClaimQueue queue, IServiceScopeFactory scopes, ILogger<ClaimWorker> log) : BackgroundService
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
            log.LogWarning(ex, "Could not recover pending claims at startup");
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
                    log.LogError(ex, "Failed to process claim {ClaimId}", claimId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }
}
