using ReturnFlow.Api.Domain;

namespace ReturnFlow.Tests;

public class ReturnClaimTests
{
    private static ReturnClaim NewOpenClaim() => new()
    {
        Id = 1,
        Description = "Item arrived damaged",
        AmountRequested = 49.90m
    };

    [Fact]
    public void Decide_ByHuman_SetsStatusProcessingAndDecision()
    {
        var claim = NewOpenClaim();

        var decision = claim.Decide(7, DecisionOutcome.Approved, "Photos confirm damage", Decider.Human);

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(ProcessingState.HumanDecided, claim.Processing);
        Assert.Equal(7, decision.AgentId);
        Assert.Same(decision, claim.LatestDecision);
    }

    [Fact]
    public void Decide_ByAuto_HasNoAgent()
    {
        var claim = NewOpenClaim();

        var decision = claim.Decide(null, DecisionOutcome.Approved, null, Decider.Auto);

        Assert.Null(decision.AgentId);
        Assert.Equal(Decider.Auto, decision.DecidedBy);
        Assert.Equal(ProcessingState.AutoDecided, claim.Processing);
    }

    [Fact]
    public void Decide_Twice_Throws()
    {
        var claim = NewOpenClaim();
        claim.Decide(7, DecisionOutcome.Approved, null, Decider.Human);

        Assert.Throws<InvalidOperationException>(() =>
            claim.Decide(8, DecisionOutcome.Rejected, null, Decider.Human));
    }

    [Fact]
    public void RequestHumanReview_ReopensAnAutoDecidedClaim()
    {
        var claim = NewOpenClaim();
        claim.Decide(null, DecisionOutcome.Rejected, null, Decider.Auto);

        claim.RequestHumanReview();

        Assert.Equal(ClaimStatus.Open, claim.Status);
        Assert.Equal(ProcessingState.HumanReviewRequested, claim.Processing);
        Assert.True(claim.HumanReviewRequested);
        Assert.NotNull(claim.ReviewRequestedAt);
    }

    [Fact]
    public void RequestHumanReview_AfterReopen_KeepsBothDecisions()
    {
        var claim = NewOpenClaim();
        claim.Decide(null, DecisionOutcome.Rejected, null, Decider.Auto);
        claim.RequestHumanReview();

        claim.Decide(7, DecisionOutcome.Approved, "Customer was right", Decider.Human);

        Assert.Equal(2, claim.Decisions.Count);
        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(ProcessingState.HumanDecided, claim.Processing);
    }

    [Fact]
    public void RequestHumanReview_OnClaimStillWaiting_Throws()
    {
        var claim = NewOpenClaim();

        Assert.Throws<InvalidOperationException>(() => claim.RequestHumanReview());
    }
}
