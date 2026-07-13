using fcs.Audit.Logs.Application.Features.AuditLogRequested.Events;
using fcs.Audit.Logs.Application.Features.AuditLogRequested.Exceptions;
using fcs.Audit.Logs.Application.Features.AuditLogRequested.Mongo;
using fcs.Audit.Logs.Application.Features.AuditLogRequested.Services;
using fcs.Audit.Logs.Application.Observability;
using FluentAssertions;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Fcs.Audit.Logs.UnitTests;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task Given_PersistAsync_Called_When_AuditLogRequestedEventIsValid_Then_ShouldPersistAuditLogDocument()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, new FixedTimeProvider(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        using var metadata = JsonDocument.Parse("""
            {
              "campaignId": "cmp-1",
              "amount": 100.50,
              "authorizationToken": "must-not-persist"
            }
            """);
        var auditLogRequestedEvent = new AuditLogRequestedEvent
        {
            EventId = " event-1 ",
            OccurredAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            ServiceName = " fcs-donations ",
            Action = "DonationRequested",
            EntityName = "Donation",
            EntityId = "don-1",
            ActorId = "donor-1",
            ActorType = "Doador",
            CorrelationId = "corr-1",
            IpAddress = "127.0.0.1",
            UserAgent = "test-agent",
            Metadata = metadata.RootElement.Clone()
        };

        // Act
        await service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        var auditLog = repository.AuditLogs.Should().ContainSingle().Subject;
        auditLog.EventId.Should().Be("event-1");
        auditLog.OccurredAt.Should().Be(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        auditLog.ReceivedAt.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        auditLog.ServiceName.Should().Be("fcs-donations");
        auditLog.Action.Should().Be("DonationRequested");
        auditLog.EntityName.Should().Be("Donation");
        auditLog.EntityId.Should().Be("don-1");
        auditLog.ActorId.Should().Be("donor-1");
        auditLog.ActorType.Should().Be("Doador");
        auditLog.CorrelationId.Should().Be("corr-1");
        auditLog.IpAddress.Should().Be("127.0.0.1");
        auditLog.UserAgent.Should().Be("test-agent");
        auditLog.Metadata.Should().NotBeNull();
        auditLog.Metadata!["campaignId"].AsString.Should().Be("cmp-1");
        auditLog.Metadata["amount"].ToDouble().Should().Be(100.50);
        auditLog.Metadata.Contains("authorizationToken").Should().BeFalse();
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_AuditLogRequestedEventHasMissingRequiredFields_Then_ShouldThrowInvalidAuditLogEventException()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = new AuditLogRequestedEvent
        {
            EventId = "event-1",
            OccurredAt = DateTimeOffset.UtcNow,
            ServiceName = "fcs-donations",
            Action = "DonationRequested"
        };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>();
        repository.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Audit log eventId is required.")]
    [InlineData("", "Audit log eventId is required.")]
    [InlineData("   ", "Audit log eventId is required.")]
    public async Task Given_PersistAsync_Called_When_EventIdIsMissing_Then_ShouldThrowInvalidAuditLogEventException(
        string? eventId,
        string expectedMessage)
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with { EventId = eventId };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage(expectedMessage);
        repository.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_OccurredAtIsMissing_Then_ShouldThrowInvalidAuditLogEventException()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with { OccurredAt = default };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage("Audit log occurredAt is required.");
        repository.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Audit log serviceName is required.")]
    [InlineData("", "Audit log serviceName is required.")]
    [InlineData("   ", "Audit log serviceName is required.")]
    public async Task Given_PersistAsync_Called_When_ServiceNameIsMissing_Then_ShouldThrowInvalidAuditLogEventException(
        string? serviceName,
        string expectedMessage)
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with { ServiceName = serviceName };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage(expectedMessage);
        repository.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Audit log action is required.")]
    [InlineData("", "Audit log action is required.")]
    [InlineData("   ", "Audit log action is required.")]
    public async Task Given_PersistAsync_Called_When_ActionIsMissing_Then_ShouldThrowInvalidAuditLogEventException(
        string? action,
        string expectedMessage)
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with { Action = action };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage(expectedMessage);
        repository.AuditLogs.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Audit log entityName is required.")]
    [InlineData("", "Audit log entityName is required.")]
    [InlineData("   ", "Audit log entityName is required.")]
    public async Task Given_PersistAsync_Called_When_EntityNameIsMissing_Then_ShouldThrowInvalidAuditLogEventException(
        string? entityName,
        string expectedMessage)
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with { EntityName = entityName };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage(expectedMessage);
        repository.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_OptionalFieldsAreBlank_Then_ShouldPersistNullOptionalFields()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with
        {
            EntityId = " ",
            ActorId = "",
            ActorType = null,
            CorrelationId = "   ",
            IpAddress = null,
            UserAgent = "",
            Metadata = null
        };

        // Act
        await service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        var auditLog = repository.AuditLogs.Should().ContainSingle().Subject;
        auditLog.EntityId.Should().BeNull();
        auditLog.ActorId.Should().BeNull();
        auditLog.ActorType.Should().BeNull();
        auditLog.CorrelationId.Should().BeNull();
        auditLog.IpAddress.Should().BeNull();
        auditLog.UserAgent.Should().BeNull();
        auditLog.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_MetadataIsNotObject_Then_ShouldThrowInvalidAuditLogEventException()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        using var metadata = JsonDocument.Parse("[1, 2, 3]");
        var auditLogRequestedEvent = CreateValidEvent() with { Metadata = metadata.RootElement.Clone() };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>().WithMessage("Audit log metadata must be a JSON object when provided.");
        repository.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_MetadataHasNestedValues_Then_ShouldPersistSupportedBsonValues()
    {
        // Arrange
        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        using var metadata = JsonDocument.Parse("""
            {
              "tags": ["urgent", "audit"],
              "approved": true,
              "score": 10,
              "ratio": 0.75,
              "empty": null,
              "nested": {
                "value": "inside"
              }
            }
            """);
        var auditLogRequestedEvent = CreateValidEvent() with { Metadata = metadata.RootElement.Clone() };

        // Act
        await service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        var auditLog = repository.AuditLogs.Should().ContainSingle().Subject;
        auditLog.Metadata.Should().NotBeNull();
        auditLog.Metadata!["tags"].AsBsonArray.Select(value => value.AsString).Should().Equal("urgent", "audit");
        auditLog.Metadata["approved"].AsBoolean.Should().BeTrue();
        auditLog.Metadata["score"].ToInt64().Should().Be(10);
        auditLog.Metadata["ratio"].ToDouble().Should().Be(0.75);
        auditLog.Metadata["empty"].IsBsonNull.Should().BeTrue();
        auditLog.Metadata["nested"].AsBsonDocument["value"].AsString.Should().Be("inside");
    }

    [Fact]
    public void Given_AuditLogsTelemetry_When_ActivitySourceIsUsed_Then_ShouldExposeStableSourceName()
    {
        // Act & Assert
        AuditLogsTelemetry.ActivitySourceName.Should().Be("Fcs.Audit.Logs");
        AuditLogsTelemetry.ActivitySource.Name.Should().Be(AuditLogsTelemetry.ActivitySourceName);
    }

    [Fact]
    public async Task Given_PersistAsync_Called_When_ActivityListenerIsRegistered_Then_ShouldCreateAuditProcessingSpan()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AuditLogsTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var repository = new InMemoryAuditLogRepository();
        var service = new AuditLogService(repository, TimeProvider.System);
        var auditLogRequestedEvent = CreateValidEvent() with
        {
            EventId = "event-42",
            ServiceName = "fcs-identity",
            Action = "DonorRegistered",
            EntityName = "DonorProfile"
        };

        // Act
        await service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        var activity = activities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("audit-log persist");
        activity.Kind.Should().Be(ActivityKind.Consumer);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem("audit.event_id").Should().Be("event-42");
        activity.GetTagItem("audit.source_service").Should().Be("fcs-identity");
        activity.GetTagItem("audit.action").Should().Be("DonorRegistered");
        activity.GetTagItem("audit.entity_name").Should().Be("DonorProfile");
        activity.GetTagItem("messaging.system").Should().Be("kafka");
        activity.GetTagItem("messaging.destination.name").Should().Be("audit-log-requested");
        activity.GetTagItem("messaging.operation").Should().Be("process");
    }

    private static AuditLogRequestedEvent CreateValidEvent()
    {
        return new AuditLogRequestedEvent
        {
            EventId = "event-1",
            OccurredAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            ServiceName = "fcs-donations",
            Action = "DonationRequested",
            EntityName = "Donation"
        };
    }

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLogDocument> AuditLogs { get; } = [];

        public Task EnsureIndexesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task InsertAsync(AuditLogDocument auditLog, CancellationToken cancellationToken)
        {
            AuditLogs.Add(auditLog);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
