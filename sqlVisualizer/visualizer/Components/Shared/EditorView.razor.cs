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
        var initialQuery = UserRepository.GetUserQuery(sessionId: HomeState.SessionId) ?? HomeState.Queries[0].SQL;
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
        HomeState.NotifyStateChanged();
    }
}
