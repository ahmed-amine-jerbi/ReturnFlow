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

    /// <summary>
    /// Concurrency token, mapped to PostgreSQL's xmin system column. It changes on every update, so
    /// saving a claim that someone else changed since it was loaded fails instead of overwriting.
    /// </summary>
    public uint Version { get; set; }

    // --- Automation state ---
    public ProcessingState Processing { get; set; } = ProcessingState.Analyzing;
    public string? RoutingReason { get; set; }
    public bool HumanReviewRequested { get; set; }
    public DateTime? ReviewRequestedAt { get; set; }

    // --- AI analysis (advisory only) ---
    public string? AiCategory { get; set; }
    public double? AiRiskScore { get; set; }
    public double? AiConfidence { get; set; }
    public List<string> AiReasons { get; set; } = [];
    public string? AiModelVersion { get; set; }
    public DateTime? AnalyzedAt { get; set; }

    /// <summary>Every decision ever made on this claim (a reopened claim has more than one).</summary>
    public List<Decision> Decisions { get; set; } = [];

    [NotMapped]
    public Decision? LatestDecision =>
        Decisions.OrderByDescending(d => d.DecidedAt).ThenByDescending(d => d.Id).FirstOrDefault();

    /// <summary>True while the claim is in an agent's queue: routed to a person, or sent back for review.</summary>
    [NotMapped]
    public bool IsAwaitingAgent =>
        Processing is ProcessingState.NeedsHuman or ProcessingState.HumanReviewRequested;

    public void ApplyAnalysis(string category, double risk, double confidence,
                              IEnumerable<string> reasons, string modelVersion)
    {
        AiCategory = category;
        AiRiskScore = risk;
        AiConfidence = confidence;
        AiReasons = [.. reasons];
        AiModelVersion = modelVersion;
        AnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>The policy settles the claim right after the AI analysis.</summary>
    public Decision DecideAutomatically(DecisionOutcome outcome, string reason)
    {
        if (Processing != ProcessingState.Analyzing)
            throw new InvalidOperationException("Only a claim that is being analyzed can be decided automatically.");

        return RecordDecision(null, outcome, "Automatic decision: " + reason, Decider.Auto);
    }

    /// <summary>
    /// An agent makes the call. The AI analyzes every claim first, so an agent can only decide a claim
    /// that the policy sent to a person or that someone asked to have reviewed.
    /// </summary>
    public Decision DecideByAgent(int agentId, DecisionOutcome outcome, string? comment)
    {
        if (!IsAwaitingAgent)
            throw new InvalidOperationException("This claim is not waiting for an agent.");

        return RecordDecision(agentId, outcome, comment, Decider.Human);
    }

    public void SendToHuman(string reason)
    {
        if (Processing != ProcessingState.Analyzing)
            throw new InvalidOperationException("Only a claim that is being analyzed can be routed to an agent.");

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

    private Decision RecordDecision(int? agentId, DecisionOutcome outcome, string? comment, Decider by)
    {
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
}
