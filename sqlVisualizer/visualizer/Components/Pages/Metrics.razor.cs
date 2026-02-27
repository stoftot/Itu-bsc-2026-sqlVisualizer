using System.Net;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Metrics : ComponentBase
{
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IHttpContextAccessor Http { get; init; }
    [Inject] public required State State { get; init; }

    private string _sessionId = "";
    string[] ActionLabels = [];
    List<ChartSeries<double>> ActionData;

    string[] StepLabels = [];
    List<ChartSeries<double>> StepData;
    protected override void OnInitialized()
    {
        _sessionId = Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown";
        LoadActionMetrics();
        LoadTimeMetrics();
        StateHasChanged();
    }
    void LoadActionMetrics()
    {
        var actionCounts = MetricsHandler.GetActionCounts().OrderBy(a => a.ActionName).ToList();
        var labels = new string[actionCounts.Count];
        var values = new double[actionCounts.Count];

        for (var i = 0; i < actionCounts.Count; i++)
        {
            labels[i] = actionCounts[i].ActionName;
            values[i] = actionCounts[i].Count;
        }

        ActionLabels = labels;

        ActionData = new()
        {
            new() { Name = "Actions", Data = values },
        };

    }

    void LoadTimeMetrics()
    {
        // Optional: aggregate across all sessions
        var steps = MetricsHandler.GetTimeSpentByStep().OrderBy(a => a.Step).ToList();
        var labels = new string[steps.Count];
        var values = new double[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            labels[i] = steps[i].Step;
            values[i] = steps[i].TimeSpentMs / 1000.0;
        }


        StepLabels = labels;
        StepData = new()
        {
            new() { Name = "Steps", Data = values },
        };
    }
}