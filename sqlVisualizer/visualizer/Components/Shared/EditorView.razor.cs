using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class EditorView : ComponentBase
{
    [Inject] public required State State { get; init; }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "sql",
            Value = State.Queries[1].SQL,
            Minimap = new EditorMinimapOptions {Enabled =  false}
        };
    }
}