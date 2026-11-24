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

public class AiAnalysis
{
    public int ClaimId { get; set; }
    public string Category { get; set; } = "other";
    public double CategoryConfidence { get; set; }
    public double RiskScore { get; set; }
    public double Confidence { get; set; }
    public string RecommendedOutcome { get; set; } = "Review";
    public List<string> Reasons { get; set; } = new();
    public string ModelVersion { get; set; } = "";
}
