namespace ReturnFlow.Api.Domain;

public class Decision
{
    public int Id { get; set; }
    public int ClaimId { get; set; }
    public ReturnClaim? Claim { get; set; }

    /// <summary>Null when the decision was made automatically.</summary>
    public int? AgentId { get; set; }
    public AppUser? Agent { get; set; }

    public Decider DecidedBy { get; set; } = Decider.Human;
    public DecisionOutcome Outcome { get; set; }
    public string? Comment { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
