using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;

namespace ReturnFlow.Api.Services;

/// <summary>KPIs for D1 (last 24 hours), D7 (last 7 days) and lifetime, with the previous period for comparison.</summary>
public class StatsService(AppDbContext db)
{
    private static readonly DateTime LifetimeStart = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task<StatsResponse> GetAsync(string range, CancellationToken ct = default)
    {
        range = (range ?? "d7").ToLowerInvariant();
        if (range is not ("d1" or "d7" or "lifetime")) range = "d7";

        var now = DateTime.UtcNow;
        TimeSpan? span = range switch
        {
            "d1" => TimeSpan.FromDays(1),
            "d7" => TimeSpan.FromDays(7),
            _ => null
        };
        var from = span is null ? LifetimeStart : now - span.Value;

        var current = await ComputeAsync(from, now, ct);
        StatsCore? previous = span is null ? null : await ComputeAsync(from - span.Value, from, ct);

        return new StatsResponse
        {
            Range = range,
            GeneratedAt = now,
            From = span is null ? null : from,
            Current = current,
            Previous = previous,
            Backlog = await BacklogAsync(ct),
            Categories = await CategoriesAsync(from, now, ct),
            Timeline = await TimelineAsync(range, now, ct)
        };
    }

    private async Task<StatsCore> ComputeAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var returnsCreated = await db.Returns.CountAsync(r => r.CreatedAt >= from && r.CreatedAt < to, ct);
        var claimsFiled = await db.Claims.CountAsync(c => c.CreatedAt >= from && c.CreatedAt < to, ct);
        var aiAnalyzed = await db.Claims.CountAsync(c => c.AnalyzedAt != null && c.AnalyzedAt >= from && c.AnalyzedAt < to, ct);
        var reviewRequests = await db.Claims.CountAsync(c => c.ReviewRequestedAt != null && c.ReviewRequestedAt >= from && c.ReviewRequestedAt < to, ct);

        var decisions = await db.Decisions.AsNoTracking()
            .Where(d => d.DecidedAt >= from && d.DecidedAt < to)
            .Select(d => new
            {
                d.DecidedBy,
                d.Outcome,
                d.DecidedAt,
                ClaimCreated = d.Claim!.CreatedAt,
                Amount = d.Claim!.AmountRequested,
                Risk = d.Claim!.AiRiskScore
            })
            .ToListAsync(ct);

        var auto = decisions.Where(d => d.DecidedBy == Decider.Auto).ToList();
        var human = decisions.Where(d => d.DecidedBy == Decider.Human).ToList();
        var approved = decisions.Where(d => d.Outcome == DecisionOutcome.Approved).ToList();
        var risks = decisions.Where(d => d.Risk != null).Select(d => d.Risk!.Value).ToList();

        return new StatsCore
        {
            ReturnsCreated = returnsCreated,
            ClaimsFiled = claimsFiled,
            AiAnalyzed = aiAnalyzed,
            ClaimsProcessed = decisions.Count,
            AutoDecided = auto.Count,
            HumanDecided = human.Count,
            HumanReviewRequests = reviewRequests,
            AutomationRatePercent = Pct(auto.Count, decisions.Count),
            ApprovalRatePercent = Pct(approved.Count, decisions.Count),
            ApprovedAmount = approved.Sum(d => d.Amount),
            AvgMinutesAuto = Avg(auto.Select(d => (d.DecidedAt - d.ClaimCreated).TotalMinutes).ToList()),
            AvgMinutesHuman = Avg(human.Select(d => (d.DecidedAt - d.ClaimCreated).TotalMinutes).ToList()),
            AvgRiskScore = risks.Count == 0 ? 0 : Math.Round(risks.Average(), 3)
        };
    }

    private async Task<BacklogInfo> BacklogAsync(CancellationToken ct) => new()
    {
        AwaitingAi = await db.Claims.CountAsync(c => c.Processing == ProcessingState.Analyzing, ct),
        NeedsHuman = await db.Claims.CountAsync(c => c.Processing == ProcessingState.NeedsHuman, ct),
        ReviewRequested = await db.Claims.CountAsync(c => c.Processing == ProcessingState.HumanReviewRequested, ct)
    };

    private async Task<List<CategoryCount>> CategoriesAsync(DateTime from, DateTime to, CancellationToken ct) =>
        await db.Claims.AsNoTracking()
            .Where(c => c.AiCategory != null && c.CreatedAt >= from && c.CreatedAt < to)
            .GroupBy(c => c.AiCategory!)
            .Select(g => new CategoryCount { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

    private async Task<List<TimelinePoint>> TimelineAsync(string range, DateTime now, CancellationToken ct)
    {
        var hourly = range == "d1";
        var count = hourly ? 24 : range == "d7" ? 7 : 30;
        var start = hourly
            ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-(count - 1))
            : now.Date.AddDays(-(count - 1));

        var points = Enumerable.Range(0, count).Select(i =>
        {
            var t = hourly ? start.AddHours(i) : start.AddDays(i);
            return new TimelinePoint
            {
                Label = hourly
                    ? t.ToString("HH':00'", CultureInfo.InvariantCulture)
                    : t.ToString("MMM d", CultureInfo.InvariantCulture)
            };
        }).ToList();

        int IndexOf(DateTime t) => hourly
            ? (int)Math.Floor((t - start).TotalHours)
            : (int)Math.Floor((t.Date - start).TotalDays);

        var decisions = await db.Decisions.AsNoTracking()
            .Where(d => d.DecidedAt >= start)
            .Select(d => new { d.DecidedBy, d.DecidedAt })
            .ToListAsync(ct);
        foreach (var d in decisions)
        {
            var i = IndexOf(d.DecidedAt);
            if (i < 0 || i >= count) continue;
            if (d.DecidedBy == Decider.Auto) points[i].Auto++; else points[i].Human++;
        }

        var filed = await db.Claims.AsNoTracking()
            .Where(c => c.CreatedAt >= start)
            .Select(c => c.CreatedAt)
            .ToListAsync(ct);
        foreach (var created in filed)
        {
            var i = IndexOf(created);
            if (i >= 0 && i < count) points[i].Filed++;
        }

        return points;
    }

    private static double Pct(int part, int total) => total == 0 ? 0 : Math.Round(100.0 * part / total, 1);

    private static double Avg(List<double> values) => values.Count == 0 ? 0 : Math.Round(values.Average(), 1);
}
