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

    protected string SelectedDatabase { get; private set; }

    protected override void OnInitialized()
    {
        CurrentDatabaseContext.ActiveConnectionString = "Data Source=data/pretest.db";
        SelectedDatabase = HomeState.SelectedDatabase;
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

    private async Task OnSelectedDatabaseChanged(ChangeEventArgs e)
    {
        var databaseName = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName == SelectedDatabase)
        {
            return;
        }

        await DatabaseChanged(databaseName);
    }

    private async Task DatabaseChanged(string databaseName)
    {
        if (HomeState.Editor is not null)
        {
            var currentQuery = await HomeState.Editor.GetValue() ?? string.Empty;
            var queryToPersist = currentQuery;
            if (HomeState.SelectedDatabase == "Example Database" && HomeState.SelectedExampleQueryIndex != 0)
            {
                queryToPersist = HomeState.Queries[0].SQL;
            }

            UserRepository.SaveUserQuery(HomeState.SessionId, HomeState.SelectedDatabase, queryToPersist);
        }

        SelectedDatabase = databaseName;
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
        var savedQuery = UserRepository.GetUserQuery(HomeState.SessionId, HomeState.SelectedDatabase) ?? string.Empty;
        HomeState.Queries[0].SQL = savedQuery;
        HomeState.SelectedExampleQueryIndex = 0;
        if (HomeState.Editor is not null)
        {
            await HomeState.Editor.SetValue(savedQuery);
        }

        HomeState.CurrentEditorQuery = savedQuery;
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
        await DatabaseChanged(safeFileName);
    }
    
    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}
