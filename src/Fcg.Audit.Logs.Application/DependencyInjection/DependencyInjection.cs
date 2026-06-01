using Fcg.Audit.Logs.Application.Common.Services;
using Fcg.Audit.Logs.Application.Common.Settings;
using Fcg.Audit.Logs.Application.Features.SampleEvent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Audit.Logs.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.SectionName));
        services.AddHostedService<SampleEventConsumer>();
        services.AddSingleton<SampleNotificationService>();

        return services;
    }
}
