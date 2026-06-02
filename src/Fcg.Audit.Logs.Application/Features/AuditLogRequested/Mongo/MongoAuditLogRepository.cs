using Fcg.Audit.Logs.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested.Mongo;

[ExcludeFromCodeCoverage]
public sealed class MongoAuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLogDocument> _collection;
    private readonly ILogger<MongoAuditLogRepository> _logger;

    public MongoAuditLogRepository(IOptions<MongoDbSettings> settings, ILogger<MongoAuditLogRepository> logger)
    {
        var mongoSettings = settings.Value;
        var client = new MongoClient(mongoSettings.ConnectionString);
        var database = client.GetDatabase(mongoSettings.DatabaseName);

        _collection = database.GetCollection<AuditLogDocument>(mongoSettings.CollectionName);
        _logger = logger;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(log => log.EventId),
                new CreateIndexOptions { Name = "UX_audit_logs_eventId", Unique = true }),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys
                    .Ascending(log => log.ServiceName)
                    .Ascending(log => log.Action),
                new CreateIndexOptions { Name = "IX_audit_logs_serviceName_action" }),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys
                    .Ascending(log => log.EntityName)
                    .Ascending(log => log.EntityId),
                new CreateIndexOptions { Name = "IX_audit_logs_entityName_entityId" }),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Descending(log => log.OccurredAt),
                new CreateIndexOptions { Name = "IX_audit_logs_occurredAt" }),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(log => log.CorrelationId),
                new CreateIndexOptions { Name = "IX_audit_logs_correlationId" }),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(log => log.ActorId),
                new CreateIndexOptions { Name = "IX_audit_logs_actorId" })
        };

        await _collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    public async Task InsertAsync(AuditLogDocument auditLog, CancellationToken cancellationToken)
    {
        try
        {
            await _collection.InsertOneAsync(auditLog, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogInformation("Audit log event {EventId} already persisted; treating duplicate as success", auditLog.EventId);
        }
    }
}
