using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }
    string _current = "Custom";

    async Task SelectChanged(ChangeEventArgs e)
    {
        if (_current == "Custom")
        {
            UserRepository.SaveUserQuery(sessionId: HomeState.SessionId, query: await HomeState.Editor.GetValue());
            HomeState.Queries[0].SQL = await HomeState.Editor.GetValue();
        }
        _current = e.Value!.ToString()!;
        MetricsHandler.IncrementAction(HomeState.SessionId, HomeState.Queries[Int32.Parse((String)e.Value)].Type);
        var newSQL = HomeState.Queries[Int32.Parse((String)e.Value)].SQL;
        await HomeState.Editor.SetValue(newSQL);
        await RunQuery();
    }

    async Task RunQuery()
    {
        var editorContent = await HomeState.Editor.GetValue();
        MetricsHandler.RecordQuery(HomeState.SessionId, editorContent);
        HomeState.RunSQL(editorContent ?? "");
    }
    
    void StepPrevious()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.Previous);
        HomeState.PreviousStep();
        StateHasChanged();
    }
    
    void StepNext()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.Next);
        HomeState.NextStep();
        StateHasChanged();
    }
}
