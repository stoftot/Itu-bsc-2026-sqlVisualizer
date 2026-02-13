using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase
{
    [Inject] public required State State { get; init; }
    string _current = "Custom";

    async Task SelectChanged(ChangeEventArgs e)
    {
        if (_current == "Custom")
        {
            State.Queries[0].SQL = await State.Editor.GetValue();
        }
        _current = e.Value!.ToString()!;
        var newSQL = State.Queries[Int32.Parse((String)e.Value)].SQL;
        await State.Editor.SetValue(newSQL);
    }

    async Task RunQuery()
    {
        var editorContent = await State.Editor.GetValue();
        State.RunSQL(editorContent ?? "");
    }
    
    void StepPrevious()
    {
        
    }
    
    void StepNext()
    {
        
    }
}