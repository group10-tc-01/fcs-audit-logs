using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested.Mongo;

[ExcludeFromCodeCoverage]
public sealed class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    [BsonElement("eventId")]
    public string EventId { get; init; } = string.Empty;

    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; init; }

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; init; }

    [BsonElement("serviceName")]
    public string ServiceName { get; init; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; init; } = string.Empty;

    [BsonElement("entityName")]
    public string EntityName { get; init; } = string.Empty;

    [BsonElement("entityId")]
    [BsonIgnoreIfNull]
    public string? EntityId { get; init; }

    [BsonElement("actorId")]
    [BsonIgnoreIfNull]
    public string? ActorId { get; init; }

    [BsonElement("actorType")]
    [BsonIgnoreIfNull]
    public string? ActorType { get; init; }

    [BsonElement("correlationId")]
    [BsonIgnoreIfNull]
    public string? CorrelationId { get; init; }

    [BsonElement("ipAddress")]
    [BsonIgnoreIfNull]
    public string? IpAddress { get; init; }

    [BsonElement("userAgent")]
    [BsonIgnoreIfNull]
    public string? UserAgent { get; init; }

    [BsonElement("metadata")]
    [BsonIgnoreIfNull]
    public BsonDocument? Metadata { get; init; }
}
