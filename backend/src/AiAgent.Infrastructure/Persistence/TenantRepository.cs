using AiAgent.Application.Tenants;
using AiAgent.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiAgent.Infrastructure.Persistence;

public sealed class TenantRepository(AppDbContext db) : ITenantRepository
{
    public async Task<TenantPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Tenants.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new TenantDto(x.Id, x.Name, x.ContactEmail, x.Phone, x.CreatedByAdminId, x.CreatedAt))
            .ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public Task<TenantDto?> GetAsync(Guid id, CancellationToken ct) => db.Tenants.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new TenantDto(x.Id, x.Name, x.ContactEmail, x.Phone, x.CreatedByAdminId, x.CreatedAt))
        .SingleOrDefaultAsync(ct);

    public async Task AddWithAuditAsync(Tenant tenant, AuditEvent audit, CancellationToken ct)
    {
        db.Tenants.Add(tenant);
        db.AuditEvents.Add(audit);
        // EF commits both inserts in its implicit SaveChanges transaction.
        await db.SaveChangesAsync(ct);
    }
}
