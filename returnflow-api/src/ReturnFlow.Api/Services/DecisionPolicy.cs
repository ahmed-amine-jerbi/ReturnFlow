namespace ReturnFlow.Api.Services;

public enum ClaimRoute { AutoApprove, AutoReject, Human }

public record PolicyResult(ClaimRoute Route, string Reason);

/// <summary>
/// The decision policy is plain, testable code. The AI advises, but only this class decides
/// whether a claim is handled automatically or goes to a person.
/// </summary>
public static class DecisionPolicy
{
    public static PolicyResult Evaluate(
        PolicyOptions options,
        DateOnly purchaseDate,
        DateTime nowUtc,
        decimal amount,
        AiAnalysis? ai,
        double spotCheckRoll)
    {
        if (!options.AutomationEnabled)
            return new(ClaimRoute.Human, "Automation is switched off");

        // 1. Deterministic rule first: the return window.
        var daysSincePurchase = DateOnly.FromDateTime(nowUtc).DayNumber - purchaseDate.DayNumber;
        if (daysSincePurchase > options.ReturnWindowDays)
            return new(ClaimRoute.AutoReject,
                $"Purchased {daysSincePurchase} days ago, outside the {options.ReturnWindowDays}-day return window");

        // 2. No AI result means no automation.
        if (ai is null)
            return new(ClaimRoute.Human, "AI analysis was unavailable");

        // 3. Anything the AI is not sure about goes to a person.
        if (!string.Equals(ai.RecommendedOutcome, "Approve", StringComparison.OrdinalIgnoreCase))
            return new(ClaimRoute.Human, "AI asked for a human review");
        if (amount > options.AutoApproveMaxAmount)
            return new(ClaimRoute.Human, $"Amount is above the automatic limit of {options.AutoApproveMaxAmount:0}");
        if (ai.RiskScore > options.MaxRiskForAutoApprove)
            return new(ClaimRoute.Human, $"Risk score {ai.RiskScore:0.00} is above the automatic limit");
        if (ai.Confidence < options.MinConfidenceForAutoApprove)
            return new(ClaimRoute.Human, $"AI confidence {ai.Confidence:0.00} is below the automatic limit");

        // 4. Quality control: a small random share is checked by a person anyway.
        if (spotCheckRoll < options.SpotCheckRate)
            return new(ClaimRoute.Human, "Random quality spot-check");

        return new(ClaimRoute.AutoApprove,
            $"Low risk ({ai.RiskScore:0.00}), high confidence ({ai.Confidence:0.00}), amount within the automatic limit");
    }
}
