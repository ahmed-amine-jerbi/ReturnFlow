using ReturnFlow.Api.Services;

namespace ReturnFlow.Tests;

public class DecisionPolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Recent = new(2026, 10, 15);

    private static AiAnalysis GoodAi() => new()
    {
        Category = "damaged",
        RiskScore = 0.08,
        Confidence = 0.9,
        RecommendedOutcome = "Approve"
    };

    private static PolicyResult Run(PolicyOptions? options = null, DateOnly? purchase = null,
                                    decimal amount = 60, AiAnalysis? ai = null, double roll = 0.9) =>
        DecisionPolicy.Evaluate(options ?? new PolicyOptions(), purchase ?? Recent, Now, amount,
                                ai ?? GoodAi(), roll);

    [Fact]
    public void LowRiskClaim_IsApprovedAutomatically()
        => Assert.Equal(ClaimRoute.AutoApprove, Run().Route);

    [Fact]
    public void RiskAtUpdatedLimit_IsApprovedAutomatically()
    {
        var ai = GoodAi();
        ai.RiskScore = 0.34;
        Assert.Equal(ClaimRoute.AutoApprove, Run(ai: ai).Route);
    }

    [Fact]
    public void RiskAboveUpdatedLimit_GoesToHuman()
    {
        var ai = GoodAi();
        ai.RiskScore = 0.36;
        Assert.Equal(ClaimRoute.Human, Run(ai: ai).Route);
    }

    [Fact]
    public void OutsideReturnWindow_IsRejectedByRule_EvenIfAiLikesIt()
        => Assert.Equal(ClaimRoute.AutoReject, Run(purchase: new DateOnly(2026, 8, 1)).Route);

    [Fact]
    public void MissingAiResult_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, DecisionPolicy.Evaluate(new PolicyOptions(), Recent, Now, 60, null, 0.9).Route);

    [Fact]
    public void AiAskingForReview_GoesToHuman()
    {
        var ai = GoodAi();
        ai.RecommendedOutcome = "Review";
        Assert.Equal(ClaimRoute.Human, Run(ai: ai).Route);
    }

    [Fact]
    public void HighAmount_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(amount: 500).Route);

    [Fact]
    public void HighRisk_GoesToHuman()
    {
        var ai = GoodAi();
        ai.RiskScore = 0.5;
        Assert.Equal(ClaimRoute.Human, Run(ai: ai).Route);
    }

    [Fact]
    public void LowConfidence_GoesToHuman()
    {
        var ai = GoodAi();
        ai.Confidence = 0.4;
        Assert.Equal(ClaimRoute.Human, Run(ai: ai).Route);
    }

    [Fact]
    public void SpotCheck_SendsSomeApprovalsToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(roll: 0.01).Route);

    [Fact]
    public void AutomationSwitchedOff_SendsEverythingToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(new PolicyOptions { AutomationEnabled = false }).Route);
}
