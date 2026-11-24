namespace ReturnFlow.Api.Dtos;

public class StatsCore
{
    public int ReturnsCreated { get; set; }
    public int ClaimsFiled { get; set; }
    public int AiAnalyzed { get; set; }
    public int ClaimsProcessed { get; set; }      // decisions made in the window
    public int AutoDecided { get; set; }
    public int HumanDecided { get; set; }
    public int HumanReviewRequests { get; set; }  // customers or staff asking for a person
    public double AutomationRatePercent { get; set; }
    public double ApprovalRatePercent { get; set; }
    public decimal ApprovedAmount { get; set; }
    public double AvgMinutesAuto { get; set; }
    public double AvgMinutesHuman { get; set; }
    public double AvgRiskScore { get; set; }
}

public class BacklogInfo
{
    public int AwaitingAi { get; set; }
    public int NeedsHuman { get; set; }
    public int ReviewRequested { get; set; }
}

public class CategoryCount
{
    public string Category { get; set; } = "";
    public int Count { get; set; }
}

public class TimelinePoint
{
    public string Label { get; set; } = "";
    public int Filed { get; set; }
    public int Auto { get; set; }
    public int Human { get; set; }
}

public class StatsResponse
{
    public string Range { get; set; } = "d7";
    public DateTime GeneratedAt { get; set; }
    public DateTime? From { get; set; }
    public StatsCore Current { get; set; } = new();
    public StatsCore? Previous { get; set; }
    public BacklogInfo Backlog { get; set; } = new();
    public List<CategoryCount> Categories { get; set; } = new();
    public List<TimelinePoint> Timeline { get; set; } = new();
}
