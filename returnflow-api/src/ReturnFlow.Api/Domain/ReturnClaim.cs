using System.ComponentModel.DataAnnotations.Schema;

namespace ReturnFlow.Api.Domain;

public class ReturnClaim
{
    public int Id { get; set; }
    public int ReturnRequestId { get; set; }
    public ReturnRequest? ReturnRequest { get; set; }
    public string Description { get; set; } = "";
    public decimal AmountRequested { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Automation state ---
    public ProcessingState Processing { get; set; } = ProcessingState.Analyzing;
    public string? RoutingReason { get; set; }
    public bool HumanReviewRequested { get; set; }
    public DateTime? ReviewRequestedAt { get; set; }

    // --- AI analysis (advisory only) ---
    public string? AiCategory { get; set; }
    public double? AiRiskScore { get; set; }
    public double? AiConfidence { get; set; }
    public string? AiReasons { get; set; }   // reasons joined with '|'
    public string? AiModelVersion { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    /// <summary>Every decision ever made on this claim (a reopened claim has more than one).</summary>
    public List<Decision> Decisions { get; set; } = new();

    [NotMapped]
    public Decision? LatestDecision =>
        Decisions.OrderByDescending(d => d.DecidedAt).ThenByDescending(d => d.Id).FirstOrDefault();

    public void ApplyAnalysis(string category, double risk, double confidence,
                              IEnumerable<string> reasons, string modelVersion)
    {
        AiCategory = category;
        AiRiskScore = risk;
        AiConfidence = confidence;
        AiReasons = string.Join("|", reasons);
        AiModelVersion = modelVersion;
        AnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>Business rule: a claim can only be decided while it is open.</summary>
    public Decision Decide(int? agentId, DecisionOutcome outcome, string? comment, Decider by)
    {
        if (Status != ClaimStatus.Open)
            throw new InvalidOperationException("This claim has already been decided.");

        Status = outcome == DecisionOutcome.Approved ? ClaimStatus.Approved : ClaimStatus.Rejected;
        Processing = by == Decider.Auto ? ProcessingState.AutoDecided : ProcessingState.HumanDecided;

        var decision = new Decision
        {
            ClaimId = Id,
            AgentId = agentId,
            DecidedBy = by,
            Outcome = outcome,
            Comment = comment,
            DecidedAt = DateTime.UtcNow
        };
        Decisions.Add(decision);
        return decision;
    }

    public void SendToHuman(string reason)
    {
        Processing = ProcessingState.NeedsHuman;
        RoutingReason = reason;
    }

    /// <summary>A person asks for a human to look at an automatic decision.</summary>
    public void RequestHumanReview()
    {
        if (Processing != ProcessingState.AutoDecided)
            throw new InvalidOperationException("Only automatically decided claims can be sent back for human review.");

        HumanReviewRequested = true;
        ReviewRequestedAt = DateTime.UtcNow;
        Status = ClaimStatus.Open;
        Processing = ProcessingState.HumanReviewRequested;
    }
}
