namespace ReturnFlow.Api.Domain;

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AuditLog For(int? userId, string action, string entityType, int entityId) => new()
    {
        UserId = userId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId
    };
}
