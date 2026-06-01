using System.Text.Json;

namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested;

public sealed record AuditLogRequestedEvent
{
    public string? EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? ServiceName { get; init; }
    public string? Action { get; init; }
    public string? EntityName { get; init; }
    public string? EntityId { get; init; }
    public string? ActorId { get; init; }
    public string? ActorType { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public JsonElement? Metadata { get; init; }
}
