using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase
{
    [Inject] public required State State { get; init; }
    [Inject] public required MetricsHandler MetricsHandler { get; init; }
    string _current = "Custom";

    async Task SelectChanged(ChangeEventArgs e)
    {
        if (_current == "Custom")
        {
            State.Queries[0].SQL = await State.Editor.GetValue();
        }
        _current = e.Value!.ToString()!;
        MetricsHandler.IncrementAction(State.SessionId, State.Queries[Int32.Parse((String)e.Value)].Type);
        var newSQL = State.Queries[Int32.Parse((String)e.Value)].SQL;
        await State.Editor.SetValue(newSQL);
        await RunQuery();
    }

    async Task RunQuery()
    {
        var editorContent = await State.Editor.GetValue();
        MetricsHandler.RecordQuery(State.SessionId, editorContent);
        State.RunSQL(editorContent ?? "");
    }
    
    void StepPrevious()
    {
        MetricsHandler.IncrementAction(State.SessionId, ActionType.Previous);
        State.PreviousStep();
        StateHasChanged();
    }
    
    void StepNext()
    {
        MetricsHandler.IncrementAction(State.SessionId, ActionType.Next);
        State.NextStep();
        StateHasChanged();
    }
}