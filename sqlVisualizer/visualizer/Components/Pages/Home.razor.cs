using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] public required State State { get; init; }
    private string query = "";
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        query = State.Queries[0].SQL;
        /*State.RunSQL = sql =>
        {
            query = sql;
            StateHasChanged();
        }; */
        Console.WriteLine("HERE ____________");
        State.RunSQL = delegate(string sql)
        {
            query = sql;
        };
        Console.WriteLine("THERE ____________");
    }
}