using AiAgent.Application.Licensing;
using AiAgent.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiAgent.Infrastructure.Persistence;

public sealed class LicenseRepository(AppDbContext db) : ILicenseRepository
{
    public Task<License?> GetForUpdateAsync(Guid tenantId, CancellationToken ct) =>
        db.Licenses.SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);

    public Task<bool> HasActiveLicenseAsync(Guid tenantId, CancellationToken ct) =>
        db.Licenses.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Status == License.Active, ct);

    public async Task<bool> SaveChangeWithAuditAsync(AuditEvent audit, CancellationToken ct)
    {
        db.AuditEvents.Add(audit);
        try
        {
            // The tracked license UPDATE includes its original Version in the WHERE clause.
            // SaveChanges commits the update and audit together, or rolls both back.
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
