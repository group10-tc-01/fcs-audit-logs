using fcs.Audit.Logs.Application.Features.AuditLogRequested.Events;
using fcs.Audit.Logs.Application.Features.AuditLogRequested.Exceptions;
using fcs.Audit.Logs.Application.Features.AuditLogRequested.Mongo;
using fcs.Audit.Logs.Application.Observability;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;

namespace fcs.Audit.Logs.Application.Features.AuditLogRequested.Services;

public sealed class AuditLogService
{
    private static readonly string[] SensitiveMetadataKeyParts = ["password", "secret", "token", "authorization"];

    private readonly IAuditLogRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AuditLogService(IAuditLogRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task PersistAsync(AuditLogRequestedEvent @event, CancellationToken cancellationToken)
    {
        using var activity = AuditLogsTelemetry.ActivitySource.StartActivity("audit-log persist", ActivityKind.Consumer);

        SetActivityTags(activity, @event);

        try
        {
            Validate(@event);

            var auditLog = new AuditLogDocument
            {
                EventId = @event.EventId!.Trim(),
                OccurredAt = @event.OccurredAt.UtcDateTime,
                ReceivedAt = _timeProvider.GetUtcNow().UtcDateTime,
                ServiceName = @event.ServiceName!.Trim(),
                Action = @event.Action!.Trim(),
                EntityName = @event.EntityName!.Trim(),
                EntityId = NormalizeOptional(@event.EntityId),
                ActorId = NormalizeOptional(@event.ActorId),
                ActorType = NormalizeOptional(@event.ActorType),
                CorrelationId = NormalizeOptional(@event.CorrelationId),
                IpAddress = NormalizeOptional(@event.IpAddress),
                UserAgent = NormalizeOptional(@event.UserAgent),
                Metadata = BuildMetadata(@event.Metadata)
            };

            using (var mongoActivity = AuditLogsTelemetry.StartMongoInsertActivity(auditLog.EventId))
            {
                await _repository.InsertAsync(auditLog, cancellationToken);
                mongoActivity?.SetStatus(ActivityStatusCode.Ok);
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    private static void SetActivityTags(Activity? activity, AuditLogRequestedEvent @event)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("messaging.system", "kafka");
        activity.SetTag("messaging.destination.name", AuditLogsTelemetry.AuditLogRequestedTopic);
        activity.SetTag("messaging.operation", "process");
        activity.SetTag("audit.event_id", @event.EventId?.Trim());
        activity.SetTag("audit.source_service", @event.ServiceName?.Trim());
        activity.SetTag("audit.action", @event.Action?.Trim());
        activity.SetTag("audit.entity_name", @event.EntityName?.Trim());
    }

    private static void Validate(AuditLogRequestedEvent @event)
    {
        if (string.IsNullOrWhiteSpace(@event.EventId))
        {
            throw new InvalidAuditLogEventException("Audit log eventId is required.");
        }

        if (@event.OccurredAt == default)
        {
            throw new InvalidAuditLogEventException("Audit log occurredAt is required.");
        }

        if (string.IsNullOrWhiteSpace(@event.ServiceName))
        {
            throw new InvalidAuditLogEventException("Audit log serviceName is required.");
        }

        if (string.IsNullOrWhiteSpace(@event.Action))
        {
            throw new InvalidAuditLogEventException("Audit log action is required.");
        }

        if (string.IsNullOrWhiteSpace(@event.EntityName))
        {
            throw new InvalidAuditLogEventException("Audit log entityName is required.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static BsonDocument? BuildMetadata(JsonElement? metadata)
    {
        if (metadata is null || metadata.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (metadata.Value.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidAuditLogEventException("Audit log metadata must be a JSON object when provided.");
        }

        return (BsonDocument)ConvertMetadataValue(metadata.Value);
    }

    private static BsonValue ConvertMetadataValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertMetadataObject(element),
            JsonValueKind.Array => new BsonArray(element.EnumerateArray().Select(ConvertMetadataValue)),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => BsonNull.Value,
            _ => BsonNull.Value
        };
    }

    private static BsonDocument ConvertMetadataObject(JsonElement element)
    {
        var document = new BsonDocument();

        foreach (var property in element.EnumerateObject())
        {
            if (IsSensitiveKey(property.Name))
            {
                continue;
            }

            document[property.Name] = ConvertMetadataValue(property.Value);
        }

        return document;
    }

    private static bool IsSensitiveKey(string key)
    {
        return SensitiveMetadataKeyParts.Any(part => key.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}
