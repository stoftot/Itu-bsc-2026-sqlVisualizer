using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using visualizer.Repositories;
using visualizer.Models;

namespace visualizer.Components.Shared;

public partial class SchemaView : ComponentBase
{
    [Inject] SQLExecutor SQLExecutor { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }
    public required Database Database { get; set; }
    [Parameter]
    public EventCallback<Table> OnTableSelected { get; set; }

    protected override void OnInitialized()
    {
        Database = SQLExecutor.GetDatabase("Data Source=data/database.db").Result;
        StateHasChanged();
        HomeState.StateChanged += OnHomeStateChanged;
    }

    private void OnHomeStateChanged() => _ = InvokeAsync(StateHasChanged);

    private void GetDatabaseNames()
    {
        HomeState.DatabaseNames = UserRepository.GetUserDatabaseNames(HomeState.SessionId);
        HomeState.NotifyStateChanged();
        StateHasChanged();
    }

    private void DatabaseChanged(ChangeEventArgs e)
    {
        Console.WriteLine($"{e.Value}: {e.Value}");
        HomeState.SelectedDatabase = e.Value!.ToString()!;
        if (string.Equals(HomeState.SelectedDatabase, "Example Database"))
        {
            Database = SQLExecutor.GetDatabase("Data Source=data/database.db").Result;
        }
        else
        {
            Database = SQLExecutor.GetDatabase("Data Source=data/"+HomeState.SessionId+"/"+HomeState.SelectedDatabase).Result;
        }
        HomeState.NotifyStateChanged();
    }

    private async Task HandleTableClick(Table table)
    {
        await OnTableSelected.InvokeAsync(table);
    }
    
    private async Task LoadFile(InputFileChangeEventArgs e)
    {
        Directory.CreateDirectory("data/" + HomeState.SessionId);
            
        var file = e.File;
        var filePath = Path.Combine("data", HomeState.SessionId, file.Name);
        await using var sourceFileStream = file.OpenReadStream();
        await using var targetFileStream = new FileStream(filePath, FileMode.Create);
        await sourceFileStream.CopyToAsync(targetFileStream);
        targetFileStream.Close();
        UserRepository.SaveUserDatabaseName(HomeState.SessionId, file.Name);
        Console.WriteLine($"{file.Name} saved to {filePath}");
    }
    
    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}