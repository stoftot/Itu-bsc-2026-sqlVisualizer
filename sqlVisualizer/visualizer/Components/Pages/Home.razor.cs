using Microsoft.AspNetCore.Components;
using visualizer.Components.Shared;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] public required State State { get; init; }
    public QueryIlustrationView QueryView; 
    private string query = "";
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        query = State.Queries[0].SQL;
        State.RunSQL = sql =>
        {
            query = sql;
            StateHasChanged();
            QueryView.Init();
        };
    }
}