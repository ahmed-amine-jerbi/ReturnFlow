using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Data;

/// <summary>
/// Fills an empty database with about a month of realistic-looking claims so the dashboard and
/// queue are not empty in a demo. The values are generated, not real. Development only.
/// </summary>
public static class DemoDataSeeder
{
    private static readonly string[] CustomerNames = { "alice", "bob", "chloe", "dina", "emir", "farah" };

    // Same keys as the AI service's categories. Weights roughly follow how often each cause comes up.
    private static readonly string[] Categories =
    {
        "damaged", "defective", "wrong_item", "missing_parts", "not_as_described",
        "late_delivery", "not_delivered", "change_of_mind", "customer_damage", "other"
    };

    private static readonly double[] CategoryWeights = { 0.22, 0.16, 0.10, 0.06, 0.09, 0.10, 0.05, 0.12, 0.06, 0.04 };

    private static readonly string[][] Texts =
    {
        new[] { "The screen arrived cracked", "Box was crushed and the product is broken", "Corner of the item is dented" },
        new[] { "Stopped working after two days", "It does not turn on at all", "Battery drains in an hour" },
        new[] { "I received the wrong color", "You sent a different size than I ordered", "Wrong model in the box" },
        new[] { "The charger was not in the box", "Only two of the three chairs arrived", "Screws are missing" },
        new[] { "Looks different from the photos", "Material is not as described", "Smaller than the listing said" },
        new[] { "Arrived two weeks late", "Delivery took far longer than promised", "Parcel was delayed ten days" },
        new[] { "Tracking says delivered but nothing came", "The parcel is lost", "My order never arrived" },
        new[] { "I changed my mind", "I no longer need it", "Found it cheaper elsewhere" },
        new[] { "I dropped it and the screen cracked", "Spilled coffee on it", "My dog chewed the cable" },
        new[] { "Please help with my order", "I want my money back" }
    };

    private static readonly string[] Items =
        { "Wireless headphones", "Running shoes", "Laptop sleeve", "Smart watch", "Desk lamp", "Blender", "Phone case", "Backpack" };

    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        if (await db.Returns.AnyAsync()) return;

        var agent = await db.Users.FirstOrDefaultAsync(u => u.Email == "agent@returnflow.local");
        var demoCustomer = await db.Users.FirstOrDefaultAsync(u => u.Email == "customer@returnflow.local");
        if (agent is null || demoCustomer is null) return;

        var rng = new Random(2026);
        var now = DateTime.UtcNow;
        var options = config.GetSection(PolicyOptions.Section).Get<PolicyOptions>() ?? new PolicyOptions();
        var hash = BCrypt.Net.BCrypt.HashPassword(config["Seed:Password"] ?? "ChangeMe123!");

        // The one-tap demo customer is an established customer with a clean record, so a clear claim
        // filed during a demo is settled automatically. The generated history goes to other customers;
        // otherwise the risk model rightly flags the demo account as a frequent claimant.
        demoCustomer.CreatedAt = now.AddDays(-400);

        var customers = new List<AppUser>();
        foreach (var name in CustomerNames)
        {
            customers.Add(new AppUser
            {
                Email = $"{name}@demo.returnflow.local",
                PasswordHash = hash,
                Role = Role.Customer,
                CreatedAt = now.AddDays(-rng.Next(5, 600))
            });
        }
        db.Users.AddRange(customers);
        await db.SaveChangesAsync();

        const int total = 90;
        const int forcedPending = 6; // keep a few claims waiting so the review queue is not empty

        for (var i = 0; i < total; i++)
        {
            var forceHuman = i < forcedPending;
            var created = forceHuman
                ? now.AddMinutes(-(8 + i * 37))
                : now.AddMinutes(-((int)(Math.Pow(rng.NextDouble(), 1.4) * 30 * 24 * 60) + 90));

            var customer = customers[rng.Next(customers.Count)];
            var categoryIndex = PickWeighted(rng, CategoryWeights);
            var category = Categories[categoryIndex];
            var text = Texts[categoryIndex][rng.Next(Texts[categoryIndex].Length)];

            var amount = forceHuman
                ? Math.Round((decimal)(180 + rng.NextDouble() * 220), 2)
                : Math.Round((decimal)(12 + Math.Pow(rng.NextDouble(), 2.2) * 420), 2);

            var daysAgo = rng.NextDouble() < 0.12 ? rng.Next(31, 60) : rng.Next(1, 25);
            var purchase = DateOnly.FromDateTime(created.AddDays(-daysAgo));
            if (forceHuman) purchase = DateOnly.FromDateTime(created.AddDays(-rng.Next(2, 20)));

            var risk = forceHuman
                ? 0.35 + rng.NextDouble() * 0.4
                : Math.Clamp(Math.Pow(rng.NextDouble(), 2.2) * 0.9 + (amount > 250 ? 0.15 : 0), 0.01, 0.97);
            // Claims kept for the queue get an unsure category, so the policy itself routes them to an agent.
            var confidence = forceHuman ? 0.35 + rng.NextDouble() * 0.2 : 0.45 + rng.NextDouble() * 0.54;

            var reasons = new List<string>();
            if (amount > 250) reasons.Add($"High claimed amount ({amount:0})");
            if (risk > 0.45) reasons.Add($"{rng.Next(1, 4)} other claim(s) in the last 30 days");
            if (risk > 0.6) reasons.Add($"{rng.Next(1, 3)} earlier claim(s) were rejected");

            var analysis = new AiAnalysis
            {
                Category = category,
                Confidence = Math.Round(confidence, 3),
                RiskScore = Math.Round(risk, 3),
                Reasons = reasons,
                ModelVersion = "synthetic-lr-2.0"
            };

            var ret = new ReturnRequest
            {
                CustomerId = customer.Id,
                OrderReference = $"ORD-{2000 + i}",
                PurchaseDate = purchase,
                Reason = $"{Items[rng.Next(Items.Length)]}: {category.Replace('_', ' ')}",
                CreatedAt = created
            };
            var claim = new ReturnClaim
            {
                Description = text,
                AmountRequested = amount,
                CreatedAt = created
            };
            claim.ApplyAnalysis(analysis.Category, analysis.RiskScore, analysis.Confidence, analysis.Reasons, analysis.ModelVersion);
            claim.AnalyzedAt = created.AddSeconds(3);
            ret.Claims.Add(claim);

            var policy = DecisionPolicy.Evaluate(options, purchase, created, amount, analysis, rng.NextDouble());
            claim.RoutingReason = policy.Reason;

            if (policy.Route == ClaimRoute.Human)
            {
                claim.SendToHuman(policy.Reason);
                if (!forceHuman) MaybeSettleByHuman(claim, created, now, agent.Id, rng);
            }
            else
            {
                var outcome = policy.Route == ClaimRoute.AutoApprove ? DecisionOutcome.Approved : DecisionOutcome.Rejected;
                var auto = claim.DecideAutomatically(outcome, policy.Reason);
                auto.DecidedAt = created.AddSeconds(rng.Next(4, 20));

                var reviewChance = policy.Route == ClaimRoute.AutoReject ? 0.35 : 0.04;
                if (rng.NextDouble() < reviewChance)
                {
                    var requestedAt = auto.DecidedAt.AddMinutes(rng.Next(5, 600));
                    if (requestedAt > now) requestedAt = now.AddMinutes(-1);
                    claim.RequestHumanReview();
                    claim.ReviewRequestedAt = requestedAt;
                    MaybeSettleByHuman(claim, requestedAt, now, agent.Id, rng);
                }
            }

            db.Returns.Add(ret);
        }

        await db.SaveChangesAsync();
    }

    private static void MaybeSettleByHuman(ReturnClaim claim, DateTime from, DateTime now, int agentId, Random rng)
    {
        if ((now - from).TotalMinutes < 90) return; // too recent: still waiting in the queue

        var decidedAt = from.AddMinutes(rng.Next(20, 20 * 60));
        if (decidedAt > now) decidedAt = now.AddMinutes(-5);

        var outcome = rng.NextDouble() < 0.7 ? DecisionOutcome.Approved : DecisionOutcome.Rejected;
        var comment = outcome == DecisionOutcome.Approved ? "Checked the details, approved." : "Does not meet the return conditions.";
        var decision = claim.DecideByAgent(agentId, outcome, comment);
        decision.DecidedAt = decidedAt;
    }

    private static int PickWeighted(Random rng, double[] weights)
    {
        var roll = rng.NextDouble() * weights.Sum();
        var cumulative = 0.0;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative) return i;
        }
        return weights.Length - 1;
    }
}
