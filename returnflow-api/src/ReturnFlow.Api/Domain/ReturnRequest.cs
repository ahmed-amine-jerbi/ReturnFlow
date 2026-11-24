namespace ReturnFlow.Api.Domain;

public class ReturnRequest
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public AppUser? Customer { get; set; }
    public string OrderReference { get; set; } = "";
    public DateOnly PurchaseDate { get; set; }
    public string Reason { get; set; } = "";
    public ReturnStatus Status { get; set; } = ReturnStatus.Submitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ReturnClaim> Claims { get; set; } = new();
}
