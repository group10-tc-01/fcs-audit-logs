using System.Diagnostics;

namespace fcs.Audit.Logs.Application.Observability;

public static class AuditLogsTelemetry
{
    public const string ActivitySourceName = "Fcs.Audit.Logs";
    public const string AuditLogRequestedTopic = "audit-log-requested";
    public const string AuditLogsDatabaseName = "AuditLogsDb";
    public const string AuditLogsCollectionName = "audit_logs";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static Activity? StartMongoInsertActivity(string eventId)
    {
        var activity = ActivitySource.StartActivity("mongodb audit_logs insert", ActivityKind.Client);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("db.system", "mongodb");
        activity.SetTag("db.name", AuditLogsDatabaseName);
        activity.SetTag("db.collection.name", AuditLogsCollectionName);
        activity.SetTag("db.operation", "insert");
        activity.SetTag("audit.event_id", eventId);

        return activity;
    }
}
