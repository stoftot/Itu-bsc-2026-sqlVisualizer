using Microsoft.AspNetCore.Components;
using visualizer.Components.Shared;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] public required IHttpContextAccessor Http { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    private QueryIlustrationView QueryView = null!;
    private string _query = "";
    protected bool viewVisulisation = true;
    protected Table tableSelected = null;

    protected override void OnInitialized()
    {
        _query = HomeState.Queries[0].SQL;
        HomeState.LastVisualizedQuery = _query;
        HomeState.SessionId = Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown";
        HomeState.StateChanged += OnHomeStateChanged;
    }

    private void OnHomeStateChanged() => _ = InvokeAsync(StateHasChanged);

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        HomeState.RunSQL = async sql =>
        {
            _query = sql;
            HomeState.LastVisualizedQuery = sql;
            await InvokeAsync(StateHasChanged);
            await QueryView.Init();
        };
    }

    protected void HandTabledSchemaTableSelected(Table table)
    {
        viewVisulisation = false;
        tableSelected = table;
        StateHasChanged();
    }

    protected void RunQueryCallback()
    {
        viewVisulisation = true;
        StateHasChanged();
    }
    
    public void Dispose()
    {
        HomeState.StateChanged -= OnHomeStateChanged;
    }
}
