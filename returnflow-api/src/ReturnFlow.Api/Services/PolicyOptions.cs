namespace ReturnFlow.Api.Services;

/// <summary>Business thresholds that decide when a claim is handled automatically. Bound from the "Policy" config section.</summary>
public class PolicyOptions
{
    public const string Section = "Policy";

    public bool AutomationEnabled { get; set; } = true;
    public int ReturnWindowDays { get; set; } = 30;
    public decimal AutoApproveMaxAmount { get; set; } = 150m;
    public double MaxRiskForAutoApprove { get; set; } = 0.35;
    public double MinConfidenceForAutoApprove { get; set; } = 0.70;

    /// <summary>Share of would-be auto-approvals sent to a human anyway, to measure the AI's accuracy.</summary>
    public double SpotCheckRate { get; set; } = 0.05;
}
