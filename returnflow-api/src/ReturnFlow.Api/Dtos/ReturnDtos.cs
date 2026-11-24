using System.ComponentModel.DataAnnotations;
using ReturnFlow.Api.Domain;

namespace ReturnFlow.Api.Dtos;

public class CreateReturnRequest
{
    [Required, StringLength(50)]
    public string OrderReference { get; init; } = "";

    /// <summary>Format yyyy-MM-dd.</summary>
    public DateOnly PurchaseDate { get; init; }

    [Required, StringLength(500)]
    public string Reason { get; init; } = "";
}

public class CreateClaimRequest
{
    [Required, StringLength(1000)]
    public string Description { get; init; } = "";

    [Range(0.01, 100000)]
    public decimal AmountRequested { get; init; }
}

public class DecisionRequest
{
    [EnumDataType(typeof(DecisionOutcome))]
    public DecisionOutcome Outcome { get; init; }

    [StringLength(500)]
    public string? Comment { get; init; }
}

public class DecisionResponse
{
    public int Id { get; init; }
    public DecisionOutcome Outcome { get; init; }
    public Decider DecidedBy { get; init; }
    public string? Comment { get; init; }
    public DateTime DecidedAt { get; init; }

    public static DecisionResponse From(Decision d) => new()
    {
        Id = d.Id,
        Outcome = d.Outcome,
        DecidedBy = d.DecidedBy,
        Comment = d.Comment,
        DecidedAt = d.DecidedAt
    };
}

public class ClaimResponse
{
    public int Id { get; init; }
    public int ReturnRequestId { get; init; }
    public string Description { get; init; } = "";
    public decimal AmountRequested { get; init; }
    public ClaimStatus Status { get; init; }
    public ProcessingState Processing { get; init; }
    public string? RoutingReason { get; init; }
    public bool HumanReviewRequested { get; init; }
    public DateTime CreatedAt { get; init; }

    public string? AiCategory { get; init; }
    public double? AiRiskScore { get; init; }
    public double? AiConfidence { get; init; }
    public List<string> AiReasons { get; init; } = new();
    public string? AiModelVersion { get; init; }
    public DateTime? AnalyzedAt { get; init; }

    /// <summary>The current decision (latest).</summary>
    public DecisionResponse? Decision { get; init; }

    /// <summary>All decisions in order. A claim sent back for review has more than one.</summary>
    public List<DecisionResponse> History { get; init; } = new();

    public static ClaimResponse From(ReturnClaim c)
    {
        var latest = c.LatestDecision;
        return new ClaimResponse
        {
            Id = c.Id,
            ReturnRequestId = c.ReturnRequestId,
            Description = c.Description,
            AmountRequested = c.AmountRequested,
            Status = c.Status,
            Processing = c.Processing,
            RoutingReason = c.RoutingReason,
            HumanReviewRequested = c.HumanReviewRequested,
            CreatedAt = c.CreatedAt,
            AiCategory = c.AiCategory,
            AiRiskScore = c.AiRiskScore,
            AiConfidence = c.AiConfidence,
            AiReasons = string.IsNullOrEmpty(c.AiReasons) ? new() : c.AiReasons.Split('|').ToList(),
            AiModelVersion = c.AiModelVersion,
            AnalyzedAt = c.AnalyzedAt,
            Decision = latest is null ? null : DecisionResponse.From(latest),
            History = c.Decisions.OrderBy(d => d.DecidedAt).Select(DecisionResponse.From).ToList()
        };
    }
}

/// <summary>A claim with the context an agent needs in the review queue.</summary>
public class StaffClaimResponse
{
    public ClaimResponse Claim { get; init; } = new();
    public string OrderReference { get; init; } = "";
    public string Reason { get; init; } = "";
    public DateOnly PurchaseDate { get; init; }
    public string CustomerEmail { get; init; } = "";

    public static StaffClaimResponse From(ReturnClaim c) => new()
    {
        Claim = ClaimResponse.From(c),
        OrderReference = c.ReturnRequest?.OrderReference ?? "",
        Reason = c.ReturnRequest?.Reason ?? "",
        PurchaseDate = c.ReturnRequest?.PurchaseDate ?? default,
        CustomerEmail = c.ReturnRequest?.Customer?.Email ?? ""
    };
}

public class ReturnResponse
{
    public int Id { get; init; }
    public string OrderReference { get; init; } = "";
    public DateOnly PurchaseDate { get; init; }
    public string Reason { get; init; } = "";
    public ReturnStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ClaimResponse> Claims { get; init; } = new();

    public static ReturnResponse From(ReturnRequest r) => new()
    {
        Id = r.Id,
        OrderReference = r.OrderReference,
        PurchaseDate = r.PurchaseDate,
        Reason = r.Reason,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        Claims = r.Claims.Select(ClaimResponse.From).ToList()
    };
}
