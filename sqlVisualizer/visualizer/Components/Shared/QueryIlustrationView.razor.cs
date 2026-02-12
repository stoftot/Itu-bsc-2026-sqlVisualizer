using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public class QueryIlustrationViewBase : ComponentBase
{
    [Parameter] public required string Query { get; init; }
    [Inject] SQLExecutor SQLExecutor { get; init; }
    public required List<Table> FromTables { get; init; } = [];
    public required Table ToTable { get; set; }

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

    private List<SQLDecompositionComponent> Steps { get; set; }
    private SQLDecompositionComponent IntialStep { get; set; }
    private SQLDecomposer Decomposer { get; } = new();

    protected override void OnInitialized()
    {
        Steps = Decomposer.Decompose(Query);
        IntialStep = Steps.First();
        Steps.Remove(IntialStep);

        UpdateStepShown();
        StateHasChanged();
    }

    private void UpdateStepShown()
    {
        FromTables.Clear();

        if (IndexOfStepToHighlight == 0)
        {
            FromTables.Add(SQLExecutor.Execute(IntialStep).Result);
        }
        else
        {
            FromTables.Add(
                SQLExecutor.Execute(
                    Steps[..IndexOfStepToHighlight].Prepend(IntialStep)
                ).Result);
        }

        var currentStep = Steps[IndexOfStepToHighlight];

        if (currentStep.Keyword.IsJoin())
        {
            FromTables.Add(SQLExecutor.Execute(currentStep.GenerateFromClauseFromJoin()).Result);
        }

        ToTable = SQLExecutor.Execute(Steps[..(IndexOfStepToHighlight + 1)].Prepend(IntialStep)).Result;

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

        for (int i = 0; i < ToTable.Entries.Count; i++)
        {
            if (i % 2 == 0) continue;
            ToTable.Entries[i].ToggleHighlight();
        }
    }

    private async Task AnimateSteps()
    {
        var animation = AnimationGenerator.Generate(FromTables, ToTable, Steps[IndexOfStepToHighlight]);

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