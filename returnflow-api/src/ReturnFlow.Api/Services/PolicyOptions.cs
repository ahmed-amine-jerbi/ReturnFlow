namespace ReturnFlow.Api.Services;

/// <summary>Business thresholds that decide when a claim is handled automatically. Bound from the "Policy" config section.</summary>
public class PolicyOptions
{
    public const string Section = "Policy";

    public bool AutomationEnabled { get; set; } = true;
    public int ReturnWindowDays { get; set; } = 30;
    public decimal AutoApproveMaxAmount { get; set; } = 600m;
    public double MaxRiskForAutoApprove { get; set; } = 0.35;

    /// <summary>The AI must be at least this sure of a covered category before a claim is approved automatically.</summary>
    public double MinConfidenceForAutoApprove { get; set; } = 0.59;

    /// <summary>
    /// The AI must be at least this sure of a not-covered category before a claim is rejected automatically.
    /// Never lower than the approval limit: a wrong rejection costs a customer more than an extra review.
    /// </summary>
    public double MinConfidenceForAutoReject { get; set; } = 0.61;

    /// <summary>Share of would-be auto-approvals sent to a human anyway, to measure the AI's accuracy.</summary>
    public double SpotCheckRate { get; set; } = 0.05;

    /// <summary>
    /// Categories the return policy covers. Set in configuration, not here, because the configuration
    /// binder appends to a non-empty default instead of replacing it. A category in neither list goes to an agent.
    /// </summary>
    public string[] CoveredCategories { get; set; } = [];

    /// <summary>Categories the return policy does not cover, such as damage the customer caused.</summary>
    public string[] NotCoveredCategories { get; set; } = [];
}
