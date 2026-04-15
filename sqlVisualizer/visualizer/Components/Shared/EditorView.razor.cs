using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class EditorView : ComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        var initialQuery = UserRepository.GetUserQuery(HomeState.SessionId, HomeState.SelectedDatabase) ?? HomeState.InitQuery.SQL;
        HomeState.Queries[0].SQL = initialQuery;
        HomeState.CurrentEditorQuery = initialQuery;

        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "sql",
            Value = initialQuery,
            Minimap = new EditorMinimapOptions {Enabled =  false}
        };
    }

    private async Task OnDidChangeModelContent(ModelContentChangedEvent _)
    {
        HomeState.CurrentEditorQuery = await HomeState.Editor.GetValue() ?? string.Empty;
        if (HomeState.SelectedDatabase == "Example Database" && HomeState.SelectedExampleQueryIndex == 0)
        {
            HomeState.Queries[0].SQL = HomeState.CurrentEditorQuery;
        }

        HomeState.NotifyStateChanged();
    }
}
