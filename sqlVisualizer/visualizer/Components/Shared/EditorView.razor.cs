using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class EditorView : ComponentBase
{
    [Inject] public required State State { get; init; }
    [Inject] public required UserRepository UserRepository { get; init; }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "sql",
            Value = UserRepository.GetUserQuery(sessionId: State.SessionId) ?? State.Queries[0].SQL,
            Minimap = new EditorMinimapOptions {Enabled =  false}
        };
    }
}