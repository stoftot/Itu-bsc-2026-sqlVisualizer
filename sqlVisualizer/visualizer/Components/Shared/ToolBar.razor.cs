using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class ToolBar : ComponentBase
{
    [Inject] public required State State { get; init; }
    string current = "Custom";
    List<Query> queries = [
        new()
        {
            Type = "Custom",
            SQL = "SELECT shift.day FROM shift"
                  
        },
        new()
        {
            Type = "Select",
            SQL = "SELECT shift.day FROM shift"
                  
        },
        new()
        {
            Type = "Join",
            SQL = "SELECT shift.day, user.email FROM shift " +
                  "JOIN user ON shift.cashier = user.username",
        },
        new()
        {
            Type = "Group By",
            SQL = "SELECT productname, count(purchasetime) FROM purchase " +
                  "GROUP BY productname",
        }
    ];

    void SelectChanged(ChangeEventArgs e)
    {
        current = e.Value!.ToString()!;
        Console.WriteLine("sql: " + queries[Int32.Parse((String)e.Value)].SQL);
    }

    void RunQuery()
    {
        Console.WriteLine("run query:");
        Console.WriteLine(State.EditorContent);
    }
    
    void StepPrevious()
    {
        
    }
    
    void StepNext()
    {
        
    }
}