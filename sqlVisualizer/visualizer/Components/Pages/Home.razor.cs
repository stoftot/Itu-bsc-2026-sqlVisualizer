using Microsoft.AspNetCore.Components;
using visualizer.Components.Shared;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] public required IHttpContextAccessor Http { get; init; }
    [Inject] public required State State { get; init; }
    private QueryIlustrationView QueryView; 
    private string query = "";


    protected override void OnInitialized()
    {
        query = State.Queries[0].SQL;
        State.SessionId = Http.HttpContext?.Request.Cookies["session_id"] ?? "unknown";
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;
        State.RunSQL = sql =>
        {
            query = sql;
            StateHasChanged();
            QueryView.Init();
        };
    }
}
