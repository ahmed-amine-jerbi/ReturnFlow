using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;

namespace ReturnFlow.Api.Services;

/// <summary>Analyzes one claim with the AI service, applies the decision policy, and saves the outcome.</summary>
public partial class ClaimProcessor(AppDbContext db, AiClient ai, IOptions<PolicyOptions> options, ILogger<ClaimProcessor> log)
{
    public async Task ProcessAsync(int claimId, CancellationToken ct)
    {
        var claim = await db.Claims
            .Include(c => c.ReturnRequest)
            .Include(c => c.Decisions)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);

        if (claim is null || claim.Processing != ProcessingState.Analyzing) return;

        var customerId = claim.ReturnRequest!.CustomerId;
        var owner = await db.Users.AsNoTracking().FirstAsync(u => u.Id == customerId, ct);

        var since = DateTime.UtcNow.AddDays(-30);
        var history = await db.Claims.AsNoTracking()
            .Where(c => c.ReturnRequest!.CustomerId == customerId && c.Id != claimId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                PreviousClaims = g.Count(),
                PreviousRejected = g.Count(c => c.Status == ClaimStatus.Rejected),
                ClaimsLast30Days = g.Count(c => c.CreatedAt >= since)
            })
            .SingleOrDefaultAsync(ct);

        var request = new AiRequest
        {
            ClaimId = claim.Id,
            Description = claim.Description,
            AmountRequested = claim.AmountRequested,
            History = new AiHistory
            {
                PreviousClaims = history?.PreviousClaims ?? 0,
                PreviousRejected = history?.PreviousRejected ?? 0,
                AccountAgeDays = (int)(DateTime.UtcNow - owner.CreatedAt).TotalDays,
                ClaimsLast30Days = history?.ClaimsLast30Days ?? 0
            }
        };

        var analysis = await ai.AnalyzeAsync(request, ct);
        if (analysis is not null)
        {
            claim.ApplyAnalysis(analysis.Category, analysis.RiskScore, analysis.Confidence,
                                analysis.Reasons, analysis.ModelVersion);
            db.AuditLogs.Add(AuditLog.For(null, "AiAnalyzed", nameof(ReturnClaim), claim.Id));
        }

        var policy = DecisionPolicy.Evaluate(
            options.Value, claim.ReturnRequest.PurchaseDate, DateTime.UtcNow,
            claim.AmountRequested, analysis, Random.Shared.NextDouble());

        claim.RoutingReason = policy.Reason;

        switch (policy.Route)
        {
            case ClaimRoute.AutoApprove:
                claim.DecideAutomatically(DecisionOutcome.Approved, policy.Reason);
                db.AuditLogs.Add(AuditLog.For(null, "AutoDecision:Approved", nameof(ReturnClaim), claim.Id));
                break;
            case ClaimRoute.AutoReject:
                claim.DecideAutomatically(DecisionOutcome.Rejected, policy.Reason);
                db.AuditLogs.Add(AuditLog.For(null, "AutoDecision:Rejected", nameof(ReturnClaim), claim.Id));
                break;
            default:
                claim.SendToHuman(policy.Reason);
                db.AuditLogs.Add(AuditLog.For(null, "SentToHuman", nameof(ReturnClaim), claim.Id));
                break;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else saved this claim while the AI was working, for example a second worker pass
            // over the same id. Their write stands; this result is dropped rather than overwriting it.
            LogStaleResultDropped(log, claimId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Claim {ClaimId} changed during analysis; the automatic result was dropped")]
    private static partial void LogStaleResultDropped(ILogger logger, int claimId);
}
