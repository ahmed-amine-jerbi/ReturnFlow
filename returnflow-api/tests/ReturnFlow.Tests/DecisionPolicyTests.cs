using ReturnFlow.Api.Services;

namespace ReturnFlow.Tests;

public class DecisionPolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Recent = new(2026, 10, 15);

    /// <summary>The same coverage lists as appsettings.json.</summary>
    private static PolicyOptions Options() => new()
    {
        CoveredCategories =
        [
            "damaged", "defective", "wrong_item", "missing_parts", "not_as_described",
            "late_delivery", "not_delivered", "change_of_mind"
        ],
        NotCoveredCategories = ["customer_damage"]
    };

    private static AiAnalysis Ai(string category = "damaged", double confidence = 0.9, double risk = 0.08) => new()
    {
        Category = category,
        Confidence = confidence,
        RiskScore = risk
    };

    private static PolicyResult Run(PolicyOptions? options = null, DateOnly? purchase = null,
                                    decimal amount = 60, AiAnalysis? ai = null, double roll = 0.9) =>
        DecisionPolicy.Evaluate(options ?? Options(), purchase ?? Recent, Now, amount, ai ?? Ai(), roll);

    [Fact]
    public void ConfidentLowRiskCoveredClaim_IsApprovedAutomatically()
        => Assert.Equal(ClaimRoute.AutoApprove, Run().Route);

    [Fact]
    public void ChangeOfMindWithinTheWindow_IsApprovedAutomatically()
        => Assert.Equal(ClaimRoute.AutoApprove, Run(ai: Ai("change_of_mind")).Route);

    [Fact]
    public void ConfidentCustomerDamage_IsRejectedAsNotCovered()
    {
        var result = Run(ai: Ai("customer_damage", confidence: 0.9));

        Assert.Equal(ClaimRoute.AutoReject, result.Route);
        Assert.Equal("Not covered by the return policy", result.Reason);
    }

    [Fact]
    public void CustomerDamageAtTheRejectLimit_IsRejected()
        => Assert.Equal(ClaimRoute.AutoReject, Run(ai: Ai("customer_damage", confidence: 0.61)).Route);

    [Fact]
    public void CustomerDamageBelowTheRejectLimit_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(ai: Ai("customer_damage", confidence: 0.60)).Route);

    [Fact]
    public void CustomerDamage_IsRejectedEvenWhenTheRiskIsHigh()
        => Assert.Equal(ClaimRoute.AutoReject, Run(ai: Ai("customer_damage", risk: 0.9)).Route);

    [Fact]
    public void CoveredClaimAtTheApproveLimit_IsApproved()
        => Assert.Equal(ClaimRoute.AutoApprove, Run(ai: Ai(confidence: 0.59)).Route);

    [Fact]
    public void CoveredClaimBelowTheApproveLimit_GoesToHuman()
    {
        var result = Run(ai: Ai(confidence: 0.58));

        Assert.Equal(ClaimRoute.Human, result.Route);
        Assert.StartsWith("AI is not sure what this claim is about", result.Reason);
    }

    [Fact]
    public void OtherCategory_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(ai: Ai("other", confidence: 0.99)).Route);

    [Fact]
    public void UnknownCategory_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(ai: Ai("something_new", confidence: 0.99)).Route);

    [Fact]
    public void EmptyCoverageLists_SendEverythingToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(new PolicyOptions()).Route);

    [Fact]
    public void OutsideReturnWindow_IsRejectedByRule_EvenIfTheClaimIsCovered()
        => Assert.Equal(ClaimRoute.AutoReject, Run(purchase: new DateOnly(2026, 8, 1)).Route);

    [Fact]
    public void MissingAiResult_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, DecisionPolicy.Evaluate(Options(), Recent, Now, 60, null, 0.9).Route);

    [Fact]
    public void AmountAboveTheLimit_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(amount: 601).Route);

    [Fact]
    public void RiskAtTheLimit_IsApproved()
        => Assert.Equal(ClaimRoute.AutoApprove, Run(ai: Ai(risk: 0.35)).Route);

    [Fact]
    public void RiskAboveTheLimit_GoesToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(ai: Ai(risk: 0.36)).Route);

    [Fact]
    public void SpotCheck_SendsSomeApprovalsToHuman()
        => Assert.Equal(ClaimRoute.Human, Run(roll: 0.01).Route);

    [Fact]
    public void AutomationSwitchedOff_SendsEverythingToHuman()
    {
        var options = Options();
        options.AutomationEnabled = false;

        Assert.Equal(ClaimRoute.Human, Run(options).Route);
    }
}
