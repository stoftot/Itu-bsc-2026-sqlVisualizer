using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using visualizer.Repositories;
using visualizer.Models;

namespace visualizer.Components.Shared;

public partial class SchemaView : ComponentBase, IDisposable
{
    [Inject] public required SQLExecutor SQLExecutor { get; init; }
    [Inject] public required ICurrentDatabaseContext CurrentDatabaseContext { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] public required IUserRepository UserRepository { get; init; }
    public required Database Database { get; set; }
    [Parameter] public EventCallback<Table> OnTableSelected { get; set; }

    private string _selectedDatabase = "";

    protected string SelectedDatabase
    {
        get => _selectedDatabase;
        set
        {
            _selectedDatabase = value;
            DatabaseChanged(value);
        }
    }

    protected override void OnInitialized()
    {
        CurrentDatabaseContext.ActiveConnectionString = "Data Source=data/database.db";
        _selectedDatabase = HomeState.SelectedDatabase;
        HomeState.DatabaseNames = UserRepository.GetUserDatabaseNames(HomeState.SessionId);
        Database = SQLExecutor.GetDatabase().Result;
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

    private void DatabaseChanged(string databaseName)
    {
        HomeState.SelectedDatabase = databaseName;
        if (string.Equals(HomeState.SelectedDatabase, "Example Database"))
        {
            CurrentDatabaseContext.ActiveConnectionString = "Data Source=data/database.db";
        }
        else if (string.Equals(HomeState.SelectedDatabase, "PostTest DB"))
        {
            CurrentDatabaseContext.ActiveConnectionString = "Data Source=data/posttest.db";
        }
        else if (string.Equals(HomeState.SelectedDatabase, "PreTest DB"))
        {
            CurrentDatabaseContext.ActiveConnectionString = "Data Source=data/pretest.db";
        }
        else
        {
            var safeFileName = Path.GetFileName(HomeState.SelectedDatabase);
            CurrentDatabaseContext.ActiveConnectionString = $"Data Source=data/{HomeState.SessionId}/{safeFileName}";
        }

        Database = SQLExecutor.GetDatabase().Result;
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
        var safeFileName = Path.GetFileName(file.Name);
        var filePath = Path.Combine("data", HomeState.SessionId, safeFileName);
        if (file.Size > 50000000)
            throw new Exception("File is too large. Maximum allowed size is 50MB.");
        
        await using var sourceFileStream = file.OpenReadStream(50000000);
        await using var targetFileStream = new FileStream(filePath, FileMode.Create);
        await sourceFileStream.CopyToAsync(targetFileStream);
        targetFileStream.Close();
        UserRepository.SaveUserDatabaseName(HomeState.SessionId, safeFileName);
        HomeState.DatabaseNames = UserRepository.GetUserDatabaseNames(HomeState.SessionId);
        SelectedDatabase = safeFileName;
    }
    
    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}
