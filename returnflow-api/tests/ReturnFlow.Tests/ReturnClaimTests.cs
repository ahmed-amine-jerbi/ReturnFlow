using ReturnFlow.Api.Domain;

namespace ReturnFlow.Tests;

public class ReturnClaimTests
{
    private static ReturnClaim NewClaim() => new()
    {
        Id = 1,
        Description = "Item arrived damaged",
        AmountRequested = 49.90m
    };

    private static ReturnClaim ClaimInAgentQueue()
    {
        var claim = NewClaim();
        claim.SendToHuman("AI asked for a human review");
        return claim;
    }

    [Fact]
    public void NewClaim_StartsInAnalysis()
    {
        var claim = NewClaim();

        Assert.Equal(ProcessingState.Analyzing, claim.Processing);
        Assert.Equal(ClaimStatus.Open, claim.Status);
    }

    [Fact]
    public void DecideAutomatically_DuringAnalysis_RecordsAnAutoDecision()
    {
        var claim = NewClaim();

        var decision = claim.DecideAutomatically(DecisionOutcome.Approved, "Low risk");

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(ProcessingState.AutoDecided, claim.Processing);
        Assert.Equal(Decider.Auto, decision.DecidedBy);
        Assert.Null(decision.AgentId);
        Assert.Equal("Automatic decision: Low risk", decision.Comment);
    }

    [Fact]
    public void DecideAutomatically_AfterRoutingToAnAgent_Throws()
    {
        var claim = ClaimInAgentQueue();

        Assert.Throws<InvalidOperationException>(() => claim.DecideAutomatically(DecisionOutcome.Approved, "Low risk"));
    }

    [Fact]
    public void DecideByAgent_InTheQueue_SetsStatusProcessingAndDecision()
    {
        var claim = ClaimInAgentQueue();

        var decision = claim.DecideByAgent(7, DecisionOutcome.Approved, "Photos confirm damage");

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(ProcessingState.HumanDecided, claim.Processing);
        Assert.Equal(7, decision.AgentId);
        Assert.Same(decision, claim.LatestDecision);
    }

    [Fact]
    public void DecideByAgent_WhileTheAiIsStillAnalyzing_Throws()
    {
        var claim = NewClaim();

        Assert.Throws<InvalidOperationException>(() => claim.DecideByAgent(7, DecisionOutcome.Approved, null));
    }

    [Fact]
    public void DecideByAgent_Twice_Throws()
    {
        var claim = ClaimInAgentQueue();
        claim.DecideByAgent(7, DecisionOutcome.Approved, null);

        Assert.Throws<InvalidOperationException>(() => claim.DecideByAgent(8, DecisionOutcome.Rejected, null));
    }

    [Fact]
    public void DecideByAgent_OnAnAutoDecidedClaimWithoutAReviewRequest_Throws()
    {
        var claim = NewClaim();
        claim.DecideAutomatically(DecisionOutcome.Rejected, "Outside the return window");

        Assert.Throws<InvalidOperationException>(() => claim.DecideByAgent(7, DecisionOutcome.Approved, null));
    }

    [Fact]
    public void SendToHuman_AfterAnAutomaticDecision_Throws()
    {
        var claim = NewClaim();
        claim.DecideAutomatically(DecisionOutcome.Approved, "Low risk");

        Assert.Throws<InvalidOperationException>(() => claim.SendToHuman("Too late"));
    }

    [Fact]
    public void RequestHumanReview_ReopensAnAutoDecidedClaim()
    {
        var claim = NewClaim();
        claim.DecideAutomatically(DecisionOutcome.Rejected, "Outside the return window");

        claim.RequestHumanReview();

        Assert.Equal(ClaimStatus.Open, claim.Status);
        Assert.Equal(ProcessingState.HumanReviewRequested, claim.Processing);
        Assert.True(claim.HumanReviewRequested);
        Assert.NotNull(claim.ReviewRequestedAt);
        Assert.True(claim.IsAwaitingAgent);
    }

    [Fact]
    public void RequestHumanReview_ThenAgentDecides_KeepsBothDecisions()
    {
        var claim = NewClaim();
        claim.DecideAutomatically(DecisionOutcome.Rejected, "Outside the return window");
        claim.RequestHumanReview();

        claim.DecideByAgent(7, DecisionOutcome.Approved, "Customer was right");

        Assert.Equal(2, claim.Decisions.Count);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(ProcessingState.HumanDecided, claim.Processing);
    }

    [Fact]
    public void RequestHumanReview_OnClaimStillBeingAnalyzed_Throws()
    {
        var claim = NewClaim();

        Assert.Throws<InvalidOperationException>(() => claim.RequestHumanReview());
    }

    [Fact]
    public void ApplyAnalysis_KeepsReasonsAsAList()
    {
        var claim = NewClaim();

        claim.ApplyAnalysis("damaged", 0.1, 0.9, ["New account", "Very short description"], "v1");

        Assert.Equal(["New account", "Very short description"], claim.AiReasons);
    }
}
