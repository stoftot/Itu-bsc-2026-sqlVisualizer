using BlazorMonaco.Editor;
using visualizer.Models;

namespace visualizer.Repositories;

public class HomeState
{
    public required string SessionId;
    public required StandaloneCodeEditor Editor { get; set; }
    public Func<string, Task> RunSQL { get; set; } = _ => Task.CompletedTask;
    public Func<int, Task> SelectStep { get; set; } = _ => Task.CompletedTask;
    public Func<Task> NextStep { get; set; } = () => Task.CompletedTask;
    public Func<Task> PreviousStep { get; set; } = () => Task.CompletedTask;
    public Func<Task> AnimatePlay { get; set; } = () => Task.CompletedTask;
    public Func<Task> AnimatePause { get; set; } = () => Task.CompletedTask;
    public Func<Task> AnimateStepNext { get; set; } = () => Task.CompletedTask;
    public Func<Task> AnimateStepPrevious { get; set; } = () => Task.CompletedTask;
    public List<Visualisation> Steps { get; set; } = [];
    public int CurrentStepIndex { get; set; } = 0;
    public int CurrentAnimationStepIndex { get; set; } = 0;
    public int CurrentAnimationStepCount { get; set; } = 0;
    public bool IsAnimationPlaying { get; set; }
    public bool ExceptionOccured { get; set; }
    public string ExceptionMessage { get; set; } = "";
    public string CurrentEditorQuery { get; set; } = "";
    public string LastVisualizedQuery { get; set; } = "";
    public bool IsShowingLatestVisualisation => NormalizeSql(CurrentEditorQuery) == NormalizeSql(LastVisualizedQuery);
    public bool IsAnimationPaused => !IsAnimationPlaying && CurrentAnimationStepIndex > 0 && CurrentAnimationStepIndex < CurrentAnimationStepCount;
    public bool ViewSidebar { get; set; } = true;
    public event Action? StateChanged;
    public void NotifyStateChanged() => StateChanged?.Invoke();
    public string SelectedDatabase { get; set; } = "Example Database";
    public List<String> DatabaseNames = [];
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

    private static string NormalizeSql(string sql)
    {
        return sql.Replace("\r\n", "\n");
    }
}