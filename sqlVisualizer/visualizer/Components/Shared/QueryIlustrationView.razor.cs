using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public class QueryIllustrationViewBase : ComponentBase
{
    [Parameter] public required string Query { get; init; }
    [Inject] SQLExecutor SQLExecutor { get; init; }
    [Inject] State State { get; init; }
    [Inject] private MetricsConfig MetricsConfig { get; init; } = null!;
    [Inject] VisualisationsGenerator VisualisationsGenerator { get; init; }
    [Inject] public required MetricsHandler MetricsHandler { get; init; }
    public required List<Table> FromTables { get; set; }
    public required List<Table> ToTables { get; set; }
    private List<Visualisation> Steps { get; set; }
    private int _indexOfStepToHighlight = 0;
    private int IndexOfStepToHighlight
    {
        get => _indexOfStepToHighlight;

        set
        {
            if (value < 0 || value >= Steps.Count)
                _indexOfStepToHighlight = 0;
                    
            _indexOfStepToHighlight = value;
            State.CurrentStepIndex = value;
            
            if (_indexOfStepToHighlight >= 0)
                MetricsHandler.EnterStep(State.SessionId, CurrStep.Component.Keyword);
            
            UpdateStepShown();
        }
    }
    
    private Visualisation CurrStep => Steps[IndexOfStepToHighlight];
    
    protected override void OnInitialized()
    {
        State.NextStep = OnNextStep;
        State.PreviousStep = OnPreviousStep;
        State.AnimatePlay = OnAnimateSteps;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if(!firstRender) return;
        Init();
        MetricsHandler.EnterStep(State.SessionId, CurrStep.Component.Keyword);
    }

    public void Init()
    {
        Steps = VisualisationsGenerator.Generate(Query);
        State.Steps = Steps;

        UpdateStepShown();
        StateHasChanged();
    }

    private void UpdateStepShown()
    {
        FromTables = CurrStep.FromTables;
        ToTables = CurrStep.ToTables;
        StateHasChanged();
        // HighligthingAndVisiblityDemo();
    }

    //this is purley for illustration purposes 
    private void HighligthingAndVisiblityDemo()
    {
        foreach (var table in FromTables)
        {
            for(int i = 0; i < table.Entries.Count; i++)
            {
                for (int k = 0; k < table.Entries[i].Values.Count; k++)
                {
                    if (k % 2 == 0)
                    {
                        if(i%2 != 0) continue;
                        table.Entries[i].Values[k].ToggleVisible();
                    }
                    else
                    {
                        if(i%2 == 0) continue;
                        table.Entries[i].Values[k].SetHighlightHexColor("4293f5");
                        table.Entries[i].Values[k].ToggleHighlight();
                    }
                        
                }
            }
        }

        // for (int i = 0; i < ToTables.Entries.Count; i++)
        // {
        //     if (i % 2 == 0) continue;
        //     ToTables.Entries[i].ToggleHighlight();
        // }
    }

    private async Task AnimateSteps()
    {
        MetricsHandler.StartAnimation(State.SessionId);
        var animation = CurrStep.Animation;
        animation.Reset();

        while (animation.NextStep())
        {
            StateHasChanged();
            await Task.Delay(1100);
        }
        MetricsHandler.StopAnimation(State.SessionId);
    }
    
    private void OnAnimateSteps()
    {
        MetricsConfig.AnimateButtonClicks.Add(1);
        AnimateSteps();
    }

    private void OnNextStep()
    {
        MetricsConfig.NextButtonClicks.Add(1);
        IndexOfStepToHighlight++;
    }

    private void OnPreviousStep()
    {
        MetricsConfig.PrevButtonClicks.Add(1);
        IndexOfStepToHighlight--;
    }
}