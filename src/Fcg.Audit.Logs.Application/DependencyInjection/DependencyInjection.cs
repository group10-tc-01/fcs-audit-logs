using Fcg.Audit.Logs.Application.Common.Settings;
using Fcg.Audit.Logs.Application.Features.AuditLogRequested;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Audit.Logs.Application.DependencyInjection;

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
