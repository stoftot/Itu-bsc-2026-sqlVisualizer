using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase, IDisposable
{
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }
    
    [Parameter]
    public EventCallback RunQueryCallback { get; set; }

    protected override void OnInitialized()
    {
        HomeState.StateChanged += OnHomeStateChanged;
    }

    private void OnHomeStateChanged() => _ = InvokeAsync(StateHasChanged);

    async Task SelectChanged(ChangeEventArgs e)
    {
        if (HomeState.SelectedExampleQueryIndex == 0)
        {
            var currentCustomQuery = await HomeState.Editor.GetValue() ?? string.Empty;
            UserRepository.SaveUserQuery(
                sessionId: HomeState.SessionId,
                databaseName: HomeState.SelectedDatabase,
                query: currentCustomQuery);
            HomeState.Queries[0].SQL = currentCustomQuery;
        }

        var selectedIndex = Int32.Parse((string)e.Value!);
        HomeState.SelectedExampleQueryIndex = selectedIndex;
        MetricsHandler.IncrementAction(HomeState.SessionId, HomeState.Queries[selectedIndex].Type);
        var newSQL = HomeState.Queries[selectedIndex].SQL;
        await HomeState.Editor.SetValue(newSQL);
        HomeState.CurrentEditorQuery = newSQL;
        HomeState.NotifyStateChanged();
        await RunQuery();
    }

    async Task RunQuery()
    {
        await RunQueryCallback.InvokeAsync();
        var editorContent = await HomeState.Editor.GetValue() ?? "";
        UserRepository.SaveUserQuery(HomeState.SessionId, HomeState.SelectedDatabase, editorContent);
        HomeState.CurrentEditorQuery = editorContent;
        MetricsHandler.RecordQuery(HomeState.SessionId, editorContent);
        await HomeState.RunSQL(editorContent);
        HomeState.LastVisualizedQuery = editorContent;
        HomeState.NotifyStateChanged();
    }

    async Task SelectStep(int index)
    {
        await HomeState.SelectStep(index);
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
            MetricsHandler.RecordActionKeyword(HomeState.SessionId, ActionType.Animate, HomeState.Steps[HomeState.CurrentStepIndex].Component.Keyword.ToString());
            await HomeState.AnimatePlay();
        }
    }

    async Task StepAnimationPrevious()
    { 
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.AnimationPrevious);
        MetricsHandler.RecordActionKeyword(HomeState.SessionId, ActionType.AnimationPrevious, HomeState.Steps[HomeState.CurrentStepIndex].Component.Keyword.ToString());
        await HomeState.AnimateStepPrevious();
    }

    async Task StepAnimationNext()
    {
        MetricsHandler.IncrementAction(HomeState.SessionId, ActionType.AnimationNext);
        MetricsHandler.RecordActionKeyword(HomeState.SessionId, ActionType.AnimationNext, HomeState.Steps[HomeState.CurrentStepIndex].Component.Keyword.ToString());
        await HomeState.AnimateStepNext();
    }

    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}
