using Fcg.Audit.Logs.Application.Features.AuditLogRequested;
using FluentAssertions;
using MongoDB.Bson;
using System.Text.Json;
using Xunit;

namespace Fcg.Audit.Logs.Application.Tests;

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
            ServiceName = " fcg-donations ",
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
        auditLog.ServiceName.Should().Be("fcg-donations");
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
            ServiceName = "fcg-donations",
            Action = "DonationRequested"
        };

        // Act
        var act = () => service.PersistAsync(auditLogRequestedEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidAuditLogEventException>();
        repository.AuditLogs.Should().BeEmpty();
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
