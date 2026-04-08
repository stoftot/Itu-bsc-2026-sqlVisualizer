using Microsoft.AspNetCore.Components;
using BlazorMonaco.Editor;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class SqlEditor : ComponentBase
{
    [Inject] public required SQLExecutor SQLExecutor { get; init; }

    protected StandaloneCodeEditor Editor = null!;
    protected Table? tableSelected;
    protected Table? resultTable;
    protected bool viewResult = true;
    protected bool HasExecutionError;
    protected string ExecutionErrorMessage = string.Empty;
    
    private string _currentEditorQuery = string.Empty;

    protected override void OnInitialized()
    {
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "sql",
            Value = _currentEditorQuery,
            Minimap = new EditorMinimapOptions { Enabled = false }
        };
    }

    private async Task OnDidChangeModelContent(ModelContentChangedEvent _)
    {
        _currentEditorQuery = await Editor.GetValue() ?? string.Empty;
    }

    private async Task RunSqlAsync(string sql)
    {
        try
        {
            DuckDbSQLDecomposer.ValidateWithDuckDb(sql);
            HasExecutionError = false;
            ExecutionErrorMessage = string.Empty;
            resultTable = await SQLExecutor.Execute(sql);
        }
        catch (Exception e)
        {
            HasExecutionError = true;
            ExecutionErrorMessage = e.Message;
            resultTable = null;
        }

        await InvokeAsync(StateHasChanged);
    }

    protected void HandTabledSchemaTableSelected(Table table)
    {
        viewResult = false;
        tableSelected = table;
        StateHasChanged();
    }

    private async Task RunQuery()
    {
        var editorContent = await Editor.GetValue() ?? "";
        _currentEditorQuery = editorContent;
        await RunSqlAsync(editorContent);
        viewResult = true;
        StateHasChanged();
    }

    private void CloseErrorPopup()
    {
        HasExecutionError = false;
        ExecutionErrorMessage = string.Empty;
        StateHasChanged();
    }
}
