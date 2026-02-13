using System.Diagnostics.Metrics;

namespace visualizer;

public class MetricsConfig
{
    public const string ServiceName = "visualizer";
    public const string ServiceVersion = "1.0.0";

    static readonly Meter Meter = new(ServiceName, ServiceVersion);

    public readonly Counter<int> PrevButtonClicks = Meter.CreateCounter<int>("prev_clicks");
    public readonly Counter<int> NextButtonClicks = Meter.CreateCounter<int>("next_clicks");
    public readonly Counter<int> AnimateButtonClicks = Meter.CreateCounter<int>("animate_clicks");
}