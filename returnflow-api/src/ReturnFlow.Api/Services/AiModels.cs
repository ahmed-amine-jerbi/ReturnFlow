namespace ReturnFlow.Api.Services;

public class AiHistory
{
    public int PreviousClaims { get; set; }
    public int PreviousRejected { get; set; }
    public int AccountAgeDays { get; set; }
    public int ClaimsLast30Days { get; set; }
}

public class AiRequest
{
    public int ClaimId { get; set; }
    public string Description { get; set; } = "";
    public decimal AmountRequested { get; set; }
    public AiHistory History { get; set; } = new();
}

/// <summary>What the AI service found. It describes the claim; <see cref="DecisionPolicy"/> decides.</summary>
public class AiAnalysis
{
    public int ClaimId { get; set; }

    /// <summary>What the claim is about, for example "defective" or "customer_damage".</summary>
    public string Category { get; set; } = "other";

    /// <summary>Probability that <see cref="Category"/> is right, from 0 to 1.</summary>
    public double Confidence { get; set; }

    /// <summary>Probability that the claim is suspicious, from 0 to 1, based on amount and history.</summary>
    public double RiskScore { get; set; }

    public List<string> Reasons { get; set; } = [];
    public string ModelVersion { get; set; } = "";
}
