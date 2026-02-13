using BlazorMonaco.Editor;
using visualizer.Models;

namespace visualizer.Repositories;

public class State
{
    public required StandaloneCodeEditor Editor { get; set; }
    public Action<string> RunSQL { get; set; }
    public List<Query> Queries = [
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
}