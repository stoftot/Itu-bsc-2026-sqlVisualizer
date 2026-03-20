using Microsoft.AspNetCore.Components;
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
}