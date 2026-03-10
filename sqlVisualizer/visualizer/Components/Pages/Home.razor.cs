using Microsoft.AspNetCore.Components;
using visualizer.Components.Shared;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] public required IHttpContextAccessor Http { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    private QueryIlustrationView QueryView; 
    private string query = "";


    protected override void OnInitialized()
    {
        query = HomeState.Queries[0].SQL;
        HomeState.SessionId = Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown";
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        HomeState.RunSQL = sql =>
        {
            query = sql;
            StateHasChanged();
            QueryView.Init();
        };
    }
}
