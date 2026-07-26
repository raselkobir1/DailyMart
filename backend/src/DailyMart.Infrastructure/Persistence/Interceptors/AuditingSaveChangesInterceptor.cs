using System.Text.Json;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auditing;
using DailyMart.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DailyMart.Infrastructure.Persistence.Interceptors;

/// <summary>
/// On every SaveChanges: stamps CreatedAt/CreatedBy/UpdatedAt/UpdatedBy on AuditableEntity entries,
/// converts hard deletes into soft deletes (IsDeleted = true instead of a DELETE), and writes one
/// AuditLog row per affected entity - so no module has to call audit logging explicitly
/// (see CLAUDE.md §4).
///
/// AuditAction.Sold is intentionally never produced here: EF Core's ChangeTracker can't distinguish
/// "this Product row changed because of a sale" from any other update. When Module 9 (POS Sales) is
/// built, it will write that distinction explicitly rather than relying on this generic interceptor.
/// </summary>
public class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentTenantService _currentTenantService;

    public AuditingSaveChangesInterceptor(ICurrentUserService currentUserService, ICurrentTenantService currentTenantService)
    {
        _currentUserService = currentUserService;
        _currentTenantService = currentTenantService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditing(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditing(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditing(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var userName = _currentUserService.UserName;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            string? oldValues = null;
            string? newValues = null;
            string? changedColumns = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userName;
                    // Tenant of a row never changes after creation, so this only needs to run on
                    // Added, never Modified - unlike CreatedBy/UpdatedBy. Only stamps when there IS a
                    // current tenant (an ordinary authenticated request) - deliberately does NOT
                    // overwrite with a default/throw when there isn't, because TenantProvisioningService
                    // creates a brand-new tenant's first Role/User/ShopSettings during the anonymous
                    // signup request itself, before any tenant claim exists to read. It sets TenantId
                    // explicitly on those entities, and this must not clobber that.
                    if (entry.Entity is TenantOwnedEntity tenantOwnedEntity && _currentTenantService.TenantId is { } currentTenantId)
                    {
                        tenantOwnedEntity.TenantId = currentTenantId;
                    }
                    newValues = Serialize(ToDictionary(entry, useOriginalValues: false));
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userName;
                    oldValues = Serialize(ToDictionary(entry, useOriginalValues: true));
                    newValues = Serialize(ToDictionary(entry, useOriginalValues: false));
                    // Compares actual values rather than trusting IsModified: Repository<T>.Update() calls
                    // DbSet.Update() on an entity that's already tracked (loaded via GetByIdAsync earlier
                    // in the same request), which marks every scalar property IsModified regardless of
                    // whether its value actually changed - that would make ChangedColumns list every
                    // column on every update instead of only the ones genuinely edited.
                    changedColumns = Serialize(entry.Properties
                        .Where(p => !Equals(entry.OriginalValues[p.Metadata], entry.CurrentValues[p.Metadata]))
                        .Select(p => p.Metadata.Name)
                        .ToList());
                    break;

                case EntityState.Deleted:
                    // Keep the row, flag it - see AuditableEntity.IsDeleted / the soft-delete query filter.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userName;
                    oldValues = Serialize(ToDictionary(entry, useOriginalValues: true));
                    break;
            }

            auditLogs.Add(new AuditLog
            {
                // Null for global/unscoped entities (Menu, Tenant, PlatformAdmin) - AuditLog itself
                // isn't tenant-filtered (see AuditLog's doc comment), so AuditLogService filters by
                // this explicitly instead of relying on the automatic convention.
                TenantId = entry.Entity is TenantOwnedEntity auditedTenantOwnedEntity ? auditedTenantOwnedEntity.TenantId : null,
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedColumns = changedColumns,
                PerformedBy = userName,
                PerformedAt = now
            });
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private static Dictionary<string, object?> ToDictionary(EntityEntry entry, bool useOriginalValues)
    {
        var values = useOriginalValues ? entry.OriginalValues : entry.CurrentValues;
        return entry.Properties.ToDictionary(
            p => p.Metadata.Name,
            p => values[p.Metadata]);
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value);
}
