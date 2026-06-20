namespace fcs.Audit.Logs.Worker.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "Fcs.Audit.Logs";

    public bool EnableOtlpExporter { get; set; }

    public string OtlpEndpoint { get; set; } = string.Empty;

    public string OtlpAuthHeader { get; set; } = string.Empty;
}
