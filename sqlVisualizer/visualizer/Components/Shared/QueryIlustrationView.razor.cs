using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public class QueryIlustrationViewBase : ComponentBase
{
    [Parameter] public required string Query { get; init; }
    [Inject] VisualisationsGenerator VisualisationsGenerator { get; init; }
    public required List<Table> FromTables { get; set; }
    public required List<Table> ToTables { get; set; }

    private List<Visualisation> Steps { get; set; }
    
    private int _indexOfStepToHighlight = 0;
    
    private int IndexOfStepToHighlight
    {
        get => _indexOfStepToHighlight;

        set
        {
            if (value < 0 || value >= Steps.Count) return;
            _indexOfStepToHighlight = value;
            UpdateStepShown();
        }
    }
    
    private Visualisation CurrStep => Steps[IndexOfStepToHighlight];
    
    protected override void OnInitialized()
    {
        Steps = VisualisationsGenerator.Generate(Query);

        UpdateStepShown();
        StateHasChanged();
    }

    private void UpdateStepShown()
    {
        FromTables = CurrStep.FromTables;
        ToTables = CurrStep.ToTables;

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
        var animation = CurrStep.Animation;
        animation.Reset();

        while (animation.NextStep())
        {
            StateHasChanged();
            await Task.Delay(1100);
        }
    }
    
    protected void OnAnimateSteps()
    {
        AnimateSteps();
    }

    protected void OnNextStep()
    {
        IndexOfStepToHighlight++;
    }

    protected void OnPreviousStep()
    {
        IndexOfStepToHighlight--;
    }
}