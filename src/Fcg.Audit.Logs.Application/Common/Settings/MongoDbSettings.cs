namespace Fcg.Audit.Logs.Application.Common.Settings;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDbSettings";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "AuditLogsDb";
    public string CollectionName { get; init; } = "audit_logs";
}
