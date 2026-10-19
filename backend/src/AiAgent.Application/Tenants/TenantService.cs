using AiAgent.Domain;

namespace AiAgent.Application.Tenants;

public sealed record TenantDto(Guid Id, string Name, string ContactEmail, string? Phone,
    Guid CreatedByAdminId, DateTimeOffset CreatedAt);
public sealed record TenantPage(IReadOnlyList<TenantDto> Items, int TotalCount, int Page, int PageSize);

public interface ITenantRepository
{
    Task<TenantPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddWithAuditAsync(Tenant tenant, AuditEvent audit, CancellationToken cancellationToken);
}

public sealed class TenantService(ITenantRepository repository)
{
    public Task<TenantPage> ListAsync(int page, int pageSize, CancellationToken ct) =>
        repository.ListAsync(page, pageSize, ct);

    public Task<TenantDto?> GetAsync(Guid id, CancellationToken ct) => repository.GetAsync(id, ct);

    public async Task<TenantDto> CreateAsync(string name, string email, string? phone, Guid actor, CancellationToken ct)
    {
        var tenant = Tenant.Create(name, email, phone, actor);
        await repository.AddWithAuditAsync(tenant, AuditEvent.TenantCreated(tenant), ct);
        return new(tenant.Id, tenant.Name, tenant.ContactEmail, tenant.Phone, tenant.CreatedByAdminId, tenant.CreatedAt);
    }
}
