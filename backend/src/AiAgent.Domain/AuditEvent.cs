namespace AiAgent.Domain;

public sealed class AuditEvent
{
    private AuditEvent() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditEvent TenantCreated(Tenant tenant) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant.Id, ActorId = tenant.CreatedByAdminId,
        Action = "TenantCreated", CreatedAt = tenant.CreatedAt
    };
}
