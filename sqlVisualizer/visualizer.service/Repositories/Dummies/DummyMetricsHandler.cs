using commonDataModels.Models;
using visualizer.service.Models;

namespace visualizer.service.Repositories.Dummies;

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
        return new List<ActionCountDto>();
    }

    public IEnumerable<StepTimeDto> GetTimeSpentByStep()
    {
        return new List<StepTimeDto>();
    }

    public void RecordActionKeyword(string sessionId, ActionType actionType, string sqlKeyword)
    {
    }

    public List<ActionKeywordMetric> GetActionKeywordMetrics()
    {
        return [];
    }

    public void RecordAnimationViewPercentage(string sessionId, SQLKeyword keyword, double percentage)
    {
    }

    public IEnumerable<ActionCountDto> GetAnimationViewPercentages()
    {
        return new List<ActionCountDto>();
    }
}
