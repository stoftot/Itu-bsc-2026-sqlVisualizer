namespace visualizer.Models;

public class Animation(IReadOnlyList<Action> steps)
{
    private int currentStepIndex = 0;
    private IReadOnlyList<Action> Steps { get; } = steps;

    public bool NextStep()
    {
        if (currentStepIndex >= Steps.Count) return false;
        Steps[currentStepIndex]();
        currentStepIndex++;
        return true;
    }

    public void Reset() => currentStepIndex = 0;
}