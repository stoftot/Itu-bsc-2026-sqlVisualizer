using visualizer.Models;

namespace visualizer.Repositories;

public class DummyMetricsHandler : IMetricsHandler
{
    public void IncrementAction(string sessionId, ActionType actionType)
    { }

    public void RecordQuery(string sessionId, string query)
    { }

    public void PrintQueries(string sessionId)
    { }

    public void PrintActions(string sessionId)
    { }

    public void EnterStep(string sessionId, SQLKeyword step)
    { }

    public void StartAnimation(string sessionId)
    { }

    public void StopAnimation(string sessionId)
    { }

    public void PrintSessionTimings(string sessionId)
    { }

    public IEnumerable<ActionCountDto> GetActionCounts()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<StepTimeDto> GetTimeSpentByStep()
    {
        throw new NotImplementedException();
    }
}