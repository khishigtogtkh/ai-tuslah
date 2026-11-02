using AiAgent.Domain;

namespace AiAgent.Application.Licensing;

public sealed record LicenseDto(Guid Id, Guid TenantId, string Status, Guid Version,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, Guid? UpdatedByAdminId)
{
    public static LicenseDto FromLicense(License license) => new(license.Id, license.TenantId,
        license.Status, license.Version, license.CreatedAt, license.UpdatedAt, license.UpdatedByAdminId);
}

public enum LicenseUpdateOutcome { Success, NotFound, Conflict }
public sealed record LicenseUpdateResult(LicenseUpdateOutcome Outcome, LicenseDto? License = null);

public interface ILicenseRepository
{
    Task<License?> GetForUpdateAsync(Guid tenantId, CancellationToken ct);
    Task<bool> SaveChangeWithAuditAsync(AuditEvent audit, CancellationToken ct);
    Task<bool> HasActiveLicenseAsync(Guid tenantId, CancellationToken ct);
}

public sealed class LicenseService(ILicenseRepository repository)
{
    // Runtime modules can use this without restricting the admin's management endpoints.
    public Task<bool> HasActiveLicenseAsync(Guid tenantId, CancellationToken ct = default) =>
        repository.HasActiveLicenseAsync(tenantId, ct);

    public async Task<LicenseUpdateResult> UpdateAsync(Guid tenantId, string status, Guid expectedVersion,
        Guid adminId, CancellationToken ct)
    {
        if (!License.IsValidStatus(status) || expectedVersion == Guid.Empty || adminId == Guid.Empty)
            throw new ArgumentException("A valid status, version and admin identity are required.");

        var license = await repository.GetForUpdateAsync(tenantId, ct);
        if (license is null) return new(LicenseUpdateOutcome.NotFound);
        if (license.Status == status)
            return new(LicenseUpdateOutcome.Success, LicenseDto.FromLicense(license));
        if (license.Version != expectedVersion) return new(LicenseUpdateOutcome.Conflict);

        license.ChangeStatus(status, adminId);
        if (!await repository.SaveChangeWithAuditAsync(AuditEvent.LicenseChanged(license), ct))
            return new(LicenseUpdateOutcome.Conflict);
        return new(LicenseUpdateOutcome.Success, LicenseDto.FromLicense(license));
    }
}
