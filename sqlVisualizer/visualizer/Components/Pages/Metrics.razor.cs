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
    
    public List<ChartSeries<double>> Series = new()
    {
        new() { Name = "United States", Data = new double[] { 40, 20, 25, 27, 46, 60, 48, 80, 15 } },
        new() { Name = "Germany", Data = new double[] { 19, 24, 35, 13, 28, 15, 13, 16, 31 } },
        new() { Name = "Sweden", Data = new double[] { 8, 6, 11, 13, 4, 16, 10, 16, 18 } },
    };
    public string[] XAxisLabels = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };

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
        
        foreach (var a in labels)
        {
            Console.WriteLine(a);    
        }

        
        foreach (var a in values)
        {
            Console.WriteLine(a);    
        }
        
        MetricsHandler.PrintSessionTimings(Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown");
    }
}