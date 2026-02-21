using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using visualizer.Models;

namespace visualizer.Repositories;

public class State
{
    public required string SessionId;
    public required StandaloneCodeEditor Editor { get; set; }
    public Action<string> RunSQL { get; set; }
    public Action NextStep { get; set; }
    public Action PreviousStep { get; set; }
    public Action AnimatePlay { get; set; }
    public Action AnimatePause { get; set; }
    public Action AnimateStepNext { get; set; }
    public Action AnimateStepPrivious { get; set; }
    public List<Visualisation> Steps { get; set; } = [];
    public int CurrentStepIndex { get; set; } = 0;
    public List<Query> Queries = [
        new()
        {
            Type = ActionType.Custom,
            SQL = "SELECT shift.day FROM shift"
                  
        },
        new()
        {
            Type = ActionType.Select,
            SQL = "SELECT shift.day FROM shift"
                  
        },
        new()
        {
            Type = ActionType.Join,
            SQL = "SELECT shift.day, user.email FROM shift " +
                  "JOIN user ON shift.cashier = user.username",
        },
        new()
        {
            Type = ActionType.GroupBy,
            SQL = "SELECT productname, count() FROM purchase " +
                  "GROUP BY productname",
        }
    ];
}