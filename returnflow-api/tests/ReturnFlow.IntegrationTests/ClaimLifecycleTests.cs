using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;

namespace ReturnFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ClaimLifecycleTests(ApiFixture api)
{
    [Fact]
    public async Task Startup_AppliesEveryMigration()
    {
        await using var db = api.CreateDbContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task FilingAClaim_SavesItWithAnAuditRow_AndRoutesItToAnAgentWhenTheAiIsDown()
    {
        var (_, customer) = await api.CreateSignedInUserAsync(Role.Customer);

        var created = await customer.PostAsJsonAsync("/api/returns", new
        {
            orderReference = "ORD-1001",
            purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            reason = "Headphones"
        });
        var ret = await created.Content.ReadFromJsonAsync<ReturnResponse>(ApiFixture.Json);
        var filed = await customer.PostAsJsonAsync($"/api/returns/{ret!.Id}/claims", new
        {
            description = "The left earcup arrived cracked",
            amountRequested = 49.90m
        });
        var claim = await filed.Content.ReadFromJsonAsync<ClaimResponse>(ApiFixture.Json);

        Assert.Equal(HttpStatusCode.Created, filed.StatusCode);
        await using (var db = api.CreateDbContext())
        {
            Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "ClaimCreated" && a.EntityId == claim!.Id));
        }

        var routed = await WaitForAsync(claim!.Id, c => c.Processing != ProcessingState.Analyzing);
        Assert.Equal(ProcessingState.NeedsHuman, routed.Processing);
        Assert.Equal("AI analysis was unavailable", routed.RoutingReason);
    }

    [Fact]
    public async Task AgentDecision_WhileTheAiIsStillAnalyzing_IsRefused()
    {
        var (owner, _) = await api.CreateSignedInUserAsync(Role.Customer);
        var (_, agent) = await api.CreateSignedInUserAsync(Role.Agent);
        var claim = await api.CreateClaimAsync(owner.Id);

        var response = await agent.PatchAsJsonAsync($"/api/claims/{claim.Id}/decision", new { outcome = "Approved" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AgentDecision_OnAQueuedClaim_IsSaved_AndASecondDecisionIsRefused()
    {
        var (owner, _) = await api.CreateSignedInUserAsync(Role.Customer);
        var (agentUser, agent) = await api.CreateSignedInUserAsync(Role.Agent);
        var claim = await api.CreateClaimAsync(owner.Id, c => c.SendToHuman("AI asked for a human review"));

        var first = await agent.PatchAsJsonAsync($"/api/claims/{claim.Id}/decision", new { outcome = "Approved", comment = "Photos confirm it" });
        var second = await agent.PatchAsJsonAsync($"/api/claims/{claim.Id}/decision", new { outcome = "Rejected" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var db = api.CreateDbContext();
        var saved = await db.Claims.Include(c => c.Decisions).SingleAsync(c => c.Id == claim.Id);
        Assert.Equal(ClaimStatus.Approved, saved.Status);
        Assert.Equal(agentUser.Id, Assert.Single(saved.Decisions).AgentId);
    }

    [Fact]
    public async Task SavingAClaimThatChangedSinceItWasLoaded_Fails()
    {
        var (owner, _) = await api.CreateSignedInUserAsync(Role.Customer);
        var (agentUser, _) = await api.CreateSignedInUserAsync(Role.Agent);
        var claim = await api.CreateClaimAsync(owner.Id, c => c.SendToHuman("AI asked for a human review"));

        await using var first = api.CreateDbContext();
        await using var second = api.CreateDbContext();
        var firstCopy = await first.Claims.Include(c => c.Decisions).SingleAsync(c => c.Id == claim.Id);
        var secondCopy = await second.Claims.Include(c => c.Decisions).SingleAsync(c => c.Id == claim.Id);

        firstCopy.DecideByAgent(agentUser.Id, DecisionOutcome.Approved, null);
        await first.SaveChangesAsync();
        secondCopy.DecideByAgent(agentUser.Id, DecisionOutcome.Rejected, null);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task AiReasons_AreStoredAsAList()
    {
        var (owner, _) = await api.CreateSignedInUserAsync(Role.Customer);
        string[] reasons = ["High claimed amount (850)", "New account (5 day(s) old)"];
        var claim = await api.CreateClaimAsync(owner.Id, c => c.ApplyAnalysis("damaged", 0.7, 0.8, reasons, "test"));

        await using var db = api.CreateDbContext();
        var saved = await db.Claims.SingleAsync(c => c.Id == claim.Id);

        Assert.Equal(reasons, saved.AiReasons);
    }

    [Fact]
    public async Task Customer_CannotSeeAnotherCustomersClaim()
    {
        var (owner, _) = await api.CreateSignedInUserAsync(Role.Customer);
        var (_, stranger) = await api.CreateSignedInUserAsync(Role.Customer);
        var claim = await api.CreateClaimAsync(owner.Id);

        var response = await stranger.GetAsync($"/api/claims/{claim.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ReturnClaim> WaitForAsync(int claimId, Func<ReturnClaim, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            await using var db = api.CreateDbContext();
            var claim = await db.Claims.AsNoTracking().SingleAsync(c => c.Id == claimId);
            if (condition(claim) || DateTime.UtcNow > deadline) return claim;
            await Task.Delay(200);
        }
    }
}
