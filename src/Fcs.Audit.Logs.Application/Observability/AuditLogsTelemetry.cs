using System.Diagnostics;

namespace fcs.Audit.Logs.Application.Observability;

public static class AuditLogsTelemetry
{
    public const string ActivitySourceName = "Fcs.Audit.Logs";
    public const string AuditLogRequestedTopic = "audit-log-requested";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
