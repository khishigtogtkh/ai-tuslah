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

    public static AuditEvent LicenseChanged(License license) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = license.TenantId,
        ActorId = license.UpdatedByAdminId ?? throw new InvalidOperationException("License has not been changed."),
        Action = license.Status == License.Active ? "LicenseActivated" : "LicenseDeactivated",
        CreatedAt = license.UpdatedAt ?? throw new InvalidOperationException("License has not been changed.")
    };
}
