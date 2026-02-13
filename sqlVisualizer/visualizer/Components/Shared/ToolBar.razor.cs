using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase
{
    [Inject] public required State State { get; init; }
    string current = "Custom";

    async Task SelectChanged(ChangeEventArgs e)
    {
        if (current == "Custom")
        {
            State.Queries[0].SQL = await State.Editor.GetValue();
        }
        current = e.Value!.ToString()!;
        var newSQL = State.Queries[Int32.Parse((String)e.Value)].SQL;
        State.Editor.SetValue(newSQL);
    }

    async Task RunQuery()
    {
        State.RunSQL = sql =>
        {
            Console.WriteLine("yesy");
            StateHasChanged();
        };
        if (State == null || State.RunSQL == null)
        {
            Console.WriteLine("YOU GOT FUCKED");
        }
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