using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;

namespace visualizer.Components.Shared;

public partial class EditorView : ComponentBase
{
    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "sql",
            Value = "SELECT * FROM WEATHER\n"
        };
    }

}