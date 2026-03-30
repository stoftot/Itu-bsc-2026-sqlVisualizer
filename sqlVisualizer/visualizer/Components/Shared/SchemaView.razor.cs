using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using visualizer.Repositories;
using visualizer.Models;

namespace visualizer.Components.Shared;

public partial class SchemaView : ComponentBase
{
    [Inject] SQLExecutor SQLExecutor { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
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
    
    private async Task LoadFiles(InputFileChangeEventArgs e)
    {
        Directory.CreateDirectory("data/" + HomeState.SessionId);
            
        var file = e.File;
        var filePath = Path.Combine("data", HomeState.SessionId, file.Name);
        await using var sourceFileStream = file.OpenReadStream();
        await using var targetFileStream = new FileStream(filePath, FileMode.Create);
        await sourceFileStream.CopyToAsync(targetFileStream);
        targetFileStream.Close();
        Console.WriteLine($"{file.Name} saved to {filePath}");
    }
}