using fcs.Audit.Logs.Application.DependencyInjection;
using fcs.Audit.Logs.Application.Common.Settings;
using fcs.Audit.Logs.Application.Observability;
using fcs.Audit.Logs.Worker.Observability;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using System.Diagnostics.CodeAnalysis;

namespace fcs.Audit.Logs.Worker;

[ExcludeFromCodeCoverage]
public class Program
{
    protected Program() { }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddHealthChecks().AddCheck("mongodb", () => CheckMongoDb(builder.Configuration));
        AddObservability(builder.Services, builder.Configuration);
        AddSerilogLogging(builder.Services, builder.Configuration);

        var app = builder.Build();

        app.MapHealthChecks("/health");
        app.MapPrometheusScrapingEndpoint("/metrics");

        app.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args);
    }

    private static HealthCheckResult CheckMongoDb(IConfiguration configuration)
    {
        try
        {
            var settings = configuration.GetRequiredSection(MongoDbSettings.SectionName).Get<MongoDbSettings>()
                ?? throw new InvalidOperationException("MongoDbSettings must be configured.");

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(exception.Message, exception);
        }
    }

    private static IServiceCollection AddObservability(IServiceCollection services, IConfiguration configuration)
    {
        var options = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(options.ServiceName, serviceNamespace: "FCS")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment
            });

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(AuditLogsTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation();

                if (options.EnableOtlpExporter && !string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri($"{options.OtlpEndpoint}/v1/traces");
                        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(options.OtlpAuthHeader))
                        {
                            exporterOptions.Headers = $"Authorization={options.OtlpAuthHeader}";
                        }
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (options.EnableOtlpExporter && !string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri($"{options.OtlpEndpoint}/v1/metrics");
                        exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(options.OtlpAuthHeader))
                        {
                            exporterOptions.Headers = $"Authorization={options.OtlpAuthHeader}";
                        }
                    });
                }
            });

        return services;
    }

    private static IServiceCollection AddSerilogLogging(IServiceCollection services, IConfiguration configuration)
    {
        var options = new ObservabilityOptions();
        configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";

        services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", options.ServiceName)
                .Enrich.WithProperty("Environment", environment)
                .WriteTo.Console();

            if (options.EnableOtlpExporter && !string.IsNullOrWhiteSpace(options.OtlpEndpoint))
            {
                loggerConfiguration.WriteTo.OpenTelemetry(otlpOptions =>
                {
                    otlpOptions.Endpoint = $"{options.OtlpEndpoint}/v1/logs";
                    otlpOptions.Protocol = OtlpProtocol.HttpProtobuf;
                    if (!string.IsNullOrWhiteSpace(options.OtlpAuthHeader))
                    {
                        otlpOptions.Headers = new Dictionary<string, string>
                        {
                            ["Authorization"] = options.OtlpAuthHeader
                        };
                    }
                    otlpOptions.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = options.ServiceName,
                        ["service.namespace"] = "FCS",
                        ["deployment.environment"] = environment
                    };
                });
            }
        });

        return services;
    }
}
