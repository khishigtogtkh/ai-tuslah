namespace AiAgent.Domain;

public sealed class License
{
    public const string Active = "active";
    public const string Inactive = "inactive";

    private License() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Status { get; private set; } = Inactive;
    public Guid Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public Guid? UpdatedByAdminId { get; private set; }

    internal static License Create(Guid tenantId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Version = Guid.NewGuid(), CreatedAt = ToMicroseconds(createdAt)
    };

    public static bool IsValidStatus(string status) => status is Active or Inactive;

    public void ChangeStatus(string status, Guid adminId)
    {
        if (!IsValidStatus(status)) throw new ArgumentException("Invalid license status.", nameof(status));
        if (adminId == Guid.Empty) throw new ArgumentException("An admin identity is required.", nameof(adminId));
        if (Status == status) return;

        Status = status;
        Version = Guid.NewGuid();
        UpdatedAt = ToMicroseconds(DateTimeOffset.UtcNow);
        UpdatedByAdminId = adminId;
    }

    // PostgreSQL timestamps have microsecond precision; keep write/read DTOs identical.
    private static DateTimeOffset ToMicroseconds(DateTimeOffset value) => value.AddTicks(-(value.Ticks % 10));
}
