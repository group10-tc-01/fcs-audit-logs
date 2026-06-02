using Fcg.Audit.Logs.Application.Common.Settings;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Kafka;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Mongo;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Fcg.Audit.Logs.Application.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAuditLogRepository, MongoAuditLogRepository>();
        services.AddSingleton<AuditLogService>();
        services.AddHostedService<AuditLogIndexesHostedService>();
        services.AddHostedService<AuditLogRequestedEventConsumer>();

        return services;
    }
}
