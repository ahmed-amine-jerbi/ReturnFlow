namespace ReturnFlow.Api.Services;

public enum ClaimRoute { AutoApprove, AutoReject, Human }

public record PolicyResult(ClaimRoute Route, string Reason);

/// <summary>
/// The decision policy is plain, testable code. The AI says what a claim is about and how sure it is;
/// only this class decides whether a claim is approved, rejected, or goes to a person. A person is
/// involved only when the AI is unsure, the claim is risky or large, or the claim type needs one.
/// </summary>
public static class DecisionPolicy
{
    /// <summary>The AI's category for a description with no readable words, such as "asdfgh".</summary>
    public const string UnreadableCategory = "unreadable";

    public static PolicyResult Evaluate(
        PolicyOptions options,
        DateOnly purchaseDate,
        DateTime nowUtc,
        decimal amount,
        AiAnalysis? ai,
        double spotCheckRoll)
    {
        if (!options.AutomationEnabled)
            return Human("Automation is switched off");

        // 1. Deterministic rule first: the return window.
        var daysSincePurchase = DateOnly.FromDateTime(nowUtc).DayNumber - purchaseDate.DayNumber;
        if (daysSincePurchase > options.ReturnWindowDays)
            return new(ClaimRoute.AutoReject,
                $"Purchased {daysSincePurchase} days ago, outside the {options.ReturnWindowDays}-day return window");

        // 2. No AI result means no automation.
        if (ai is null)
            return Human("AI analysis was unavailable");

        // 3. A description with no readable words gives nothing to decide on.
        if (ai.Category == UnreadableCategory)
            return new(ClaimRoute.AutoReject, "The description does not say what is wrong");

        // 4. Claims the policy does not cover are rejected, but only when the AI is clearly sure.
        if (options.NotCoveredCategories.Contains(ai.Category))
            return ai.Confidence >= options.MinConfidenceForAutoReject
                ? new(ClaimRoute.AutoReject, "Not covered by the return policy")
                : Human(Uncertain(ai));

        // 5. Anything unknown or unclear ("other") is for a person.
        if (!options.CoveredCategories.Contains(ai.Category))
            return Human("This type of claim always goes to an agent");

        // 6. Covered claims are approved when the AI is sure, the risk is low and the amount is small.
        if (ai.Confidence < options.MinConfidenceForAutoApprove)
            return Human(Uncertain(ai));
        if (amount > options.AutoApproveMaxAmount)
            return Human($"Amount is above the automatic limit of {options.AutoApproveMaxAmount:0}");
        if (ai.RiskScore > options.MaxRiskForAutoApprove)
            return Human($"Risk score {ai.RiskScore:0.00} is above the automatic limit");

        // 7. Quality control: a small random share is checked by a person anyway.
        if (spotCheckRoll < options.SpotCheckRate)
            return Human("Random quality spot-check");

        return new(ClaimRoute.AutoApprove,
            $"Low risk ({ai.RiskScore:0.00}), high confidence ({ai.Confidence:0.00}), amount within the automatic limit");
    }

    private static PolicyResult Human(string reason) => new(ClaimRoute.Human, reason);

    private static string Uncertain(AiAnalysis ai) =>
        $"AI is not sure what this claim is about (confidence {ai.Confidence:0.00})";
}
