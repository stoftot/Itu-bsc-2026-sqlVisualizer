using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using visualizer.Repositories;
using visualizer.Models;

namespace visualizer.Components.Shared;

public partial class SchemaView : ComponentBase
{
    [Inject] SQLExecutor SQLExecutor { get; init; }
    public required Database Database { get; set; }
    [Parameter]
    public EventCallback<Table> OnTableSelected { get; set; }

    protected override void OnInitialized()
    {
        Database = SQLExecutor.GetDatabase().Result;
        StateHasChanged();
    }

    private async Task HandleTableClick(Table table)
    {
        await OnTableSelected.InvokeAsync(table);
    }
    
    private void LoadFiles(InputFileChangeEventArgs e)
    {
            var files = e.GetMultipleFiles();
            foreach (var file in files)
            {
                Console.WriteLine($"File selected: {file.Name}");
            }
    }
}