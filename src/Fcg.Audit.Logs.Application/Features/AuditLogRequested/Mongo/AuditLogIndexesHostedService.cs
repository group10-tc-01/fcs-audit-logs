using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Fcg.Audit.Logs.Application.Features.AuditLogRequested.Mongo;

[ExcludeFromCodeCoverage]
public sealed class AuditLogIndexesHostedService : IHostedService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogIndexesHostedService> _logger;

    public AuditLogIndexesHostedService(IAuditLogRepository repository, ILogger<AuditLogIndexesHostedService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensuring MongoDB indexes for audit_logs collection");
        await _repository.EnsureIndexesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
