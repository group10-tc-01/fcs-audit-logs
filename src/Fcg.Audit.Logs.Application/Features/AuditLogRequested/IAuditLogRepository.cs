namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested;

public interface IAuditLogRepository
{
    Task EnsureIndexesAsync(CancellationToken cancellationToken);
    Task InsertAsync(AuditLogDocument auditLog, CancellationToken cancellationToken);
}
