using AiAgent.Application.Tenants;
using AiAgent.Application.Licensing;
using AiAgent.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AiAgent.Infrastructure.Persistence;

public sealed class TenantRepository(AppDbContext db) : ITenantRepository
{
    private static readonly Expression<Func<Tenant, TenantDto>> Projection = x => new TenantDto(
        x.Id, x.Name, x.ContactEmail, x.Phone, x.CreatedByAdminId, x.CreatedAt,
        new LicenseDto(x.License.Id, x.License.TenantId, x.License.Status, x.License.Version,
            x.License.CreatedAt, x.License.UpdatedAt, x.License.UpdatedByAdminId));

    public async Task<TenantPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Tenants.AsNoTracking();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(Projection)
            .ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    public Task<TenantDto?> GetAsync(Guid id, CancellationToken ct) => db.Tenants.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(Projection)
        .SingleOrDefaultAsync(ct);

    public async Task AddWithAuditAsync(Tenant tenant, AuditEvent audit, CancellationToken ct)
    {
        db.Tenants.Add(tenant);
        db.AuditEvents.Add(audit);
        // The tenant's new License navigation is inserted in this same transaction.
        await db.SaveChangesAsync(ct);
    }
}
