using Fcg.Audit.Logs.Application.Common.Abstractions;
using Fcg.Audit.Logs.Application.Common.Settings;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Events;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Exceptions;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested.Kafka;

[ExcludeFromCodeCoverage]
public sealed class AuditLogRequestedEventConsumer : BaseKafkaConsumer<AuditLogRequestedEvent>
{
    private readonly AuditLogService _auditLogService;
    private readonly ILogger<AuditLogRequestedEventConsumer> _logger;

    public AuditLogRequestedEventConsumer(
        ILogger<AuditLogRequestedEventConsumer> logger,
        IOptions<KafkaSettings> kafkaSettings,
        AuditLogService auditLogService)
        : base(
            logger,
            kafkaSettings.Value.BootstrapServers,
            kafkaSettings.Value.GroupId,
            kafkaSettings.Value.Topics.AuditLogRequested,
            kafkaSettings.Value.ConsumerTimeoutMs)
    {
        _logger = logger;
        _auditLogService = auditLogService;
    }

    protected override async Task ProcessEventAsync(AuditLogRequestedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            await _auditLogService.PersistAsync(@event, cancellationToken);
            _logger.LogInformation("Audit log event {EventId} persisted", @event.EventId);
        }
        catch (InvalidAuditLogEventException exception)
        {
            _logger.LogWarning(exception, "Discarding invalid audit log event {EventId}", @event.EventId);
        }
    }
}
