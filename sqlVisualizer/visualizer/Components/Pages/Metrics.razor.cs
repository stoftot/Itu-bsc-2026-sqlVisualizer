using Microsoft.AspNetCore.Components;
using MudBlazor;
using visualizer.service.Repositories;

namespace visualizer.Components.Pages;

public partial class Metrics : ComponentBase
{
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IHttpContextAccessor Http { get; init; }
    [Inject] public required HomeState HomeState { get; init; }

    private string _sessionId = "";
    string[] ActionLabels = [];
    List<ChartSeries<double>> ActionData;

    string[] StepLabels = [];
    List<ChartSeries<double>> StepData;
    
    string[] AnimationLabels = [];
    List<ChartSeries<double>> AnimationData;
    
    string[] AnimationViewPercentageLabels = [];
    List<ChartSeries<double>> AnimationViewPercentageData;
    
    List<string[]> ActionKeywordLabels = [];
    List<List<ChartSeries<double>>> ActionKeywordData = [];
    
    protected override Task OnInitializedAsync()
    {
        _sessionId = Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown";
        LoadActionMetrics();
        LoadTimeMetrics();
        LoadActionKeywordMetrics();
        LoadAnimationViewPercentageMetrics();
        InvokeAsync(StateHasChanged);
        return base.OnInitializedAsync();
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
    
    void LoadActionKeywordMetrics()
    {
        var keywordCounts = MetricsHandler.GetActionKeywordMetrics().GroupBy(a => a.SqlKeyword).ToList();
    
        ActionKeywordLabels.Clear();
        ActionKeywordData.Clear();
    
        for (var i = 0; i < keywordCounts.Count; i++)
        {
            var group = keywordCounts[i];
            var actions = group.ToList();
            // Skip if no actions for this keyword
            if (actions.Count == 0)
                continue;
        
            var subLabels = new string[actions.Count];
            var subValues = new double[actions.Count];
        
            for (var j = 0; j < actions.Count; j++)
            {
                subLabels[j] = actions[j].ActionType;
                subValues[j] = actions[j].Count;
            }
        
            ActionKeywordLabels.Add(subLabels);
            ActionKeywordData.Add(new List<ChartSeries<double>>
            {
                new() { Name = group.Key, Data = subValues }
            });
        }
    }

    void LoadTimeMetrics()
    {
        // Optional: aggregate across all sessions
        var steps = MetricsHandler.GetTimeSpentByStep().OrderBy(a => a.Step).ToList();
        var stepCount = steps.Count;
        
        var labels = new string[stepCount];
        var stepValues = new double[stepCount];
        var animationValues = new double[stepCount];
        for (var i = 0; i < steps.Count; i++)
        {
            labels[i] = steps[i].Step;
            stepValues[i] = steps[i].TimeSpentMs / 1000.0;
            animationValues[i] = steps[i].AnimationMs / 1000.0;
        }


        StepLabels = labels;
        StepData = new()
        {
            new() { Name = "Steps", Data = stepValues },
        };
        AnimationData = new()
        {
            new() { Name = "Animation Time", Data = animationValues },
        };
    }

    void LoadAnimationViewPercentageMetrics()
    {
        var animationPercentages = MetricsHandler.GetAnimationViewPercentages().OrderBy(a => a.ActionName).ToList();
        var labels = new string[animationPercentages.Count];
        var values = new double[animationPercentages.Count];

        for (var i = 0; i < animationPercentages.Count; i++)
        {
            labels[i] = animationPercentages[i].ActionName;
            values[i] = animationPercentages[i].Count;
        }

        AnimationViewPercentageLabels = labels;
        AnimationViewPercentageData = new()
        {
            new() { Name = "Average Completion %", Data = values },
        };
    }

    int GetChartHeight(int itemCount)
    {
        const int baseHeight = 120;
        const int heightPerItem = 35;

        return Math.Max(300, baseHeight + itemCount * heightPerItem);
    }
}