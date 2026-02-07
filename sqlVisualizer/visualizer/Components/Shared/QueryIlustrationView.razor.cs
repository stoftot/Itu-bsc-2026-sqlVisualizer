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

        UpdateHighlightedSteps();
    }

    private void UpdateHighlightedSteps()
    {
        // foreach (var table in FromTables)
        // {
        //     for(int i = 0; i < table.Entries.Count; i++)
        //     {
        //         if(i%2 == 0) continue;
        //         table.Entries[i].IsHighlighted = !table.Entries[i].IsHighlighted;
        //         table.Entries[i].SetHighlightHexColor("4293f5");
        //     }
        // }

        for (int i = 0; i < ToTable.Entries.Count; i++)
        {
            if (i % 2 == 0) continue;
            ToTable.Entries[i].IsHighlighted = !ToTable.Entries[i].IsHighlighted;
        }
    }

    private async Task AnimateSteps()
    {
        foreach (var t in FromTables.SelectMany(table => table.Entries))
        {
            t.ToggleHighlight();
            t.SetHighlightHexColor("4293f5");
            StateHasChanged();
            await Task.Delay(1000);
            t.ToggleHighlight();
            StateHasChanged();
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