using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase, IDisposable
{
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }
    string _current = "Custom";
    
    [Parameter]
    public EventCallback RunQueryCallback { get; set; }

    protected override void OnInitialized()
    {
        HomeState.StateChanged += OnHomeStateChanged;
    }

    private void OnHomeStateChanged() => _ = InvokeAsync(StateHasChanged);

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
        HomeState.CurrentEditorQuery = newSQL;
        HomeState.NotifyStateChanged();
        await RunQuery();
    }

    async Task RunQuery()
    {
        var editorContent = await HomeState.Editor.GetValue() ?? "";
        HomeState.CurrentEditorQuery = editorContent;
        MetricsHandler.RecordQuery(HomeState.SessionId, editorContent);
        await HomeState.RunSQL(editorContent);
        HomeState.LastVisualizedQuery = editorContent;
        HomeState.NotifyStateChanged();
        await RunQueryCallback.InvokeAsync();
    }

    async Task StepPrevious()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.Previous);
        await HomeState.PreviousStep();
    }

    async Task StepNext()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.Next);
        await HomeState.NextStep();
    }

    async Task ToggleAnimation()
    {
        if (HomeState.IsAnimationPlaying)
        {
            await HomeState.AnimatePause();
        }
        else
        {
            MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.Animate);
            await HomeState.AnimatePlay();
        }
    }

    async Task StepAnimationPrevious()
    { 
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.AnimationPrevious);
        await HomeState.AnimateStepPrevious();
    }

    async Task StepAnimationNext()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.AnimationNext);
        await HomeState.AnimateStepNext();
    }

    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}
