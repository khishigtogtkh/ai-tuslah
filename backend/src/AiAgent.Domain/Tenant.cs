namespace AiAgent.Domain;

public sealed class Tenant
{
    private Tenant() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string ContactEmail { get; private set; } = "";
    public string? Phone { get; private set; }
    public Guid CreatedByAdminId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public License License { get; private set; } = null!;

    public static Tenant Create(string name, string contactEmail, string? phone, Guid adminId)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ContactEmail = contactEmail.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            CreatedByAdminId = adminId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        tenant.License = License.Create(tenant.Id, tenant.CreatedAt);
        return tenant;
    }
}
