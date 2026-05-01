namespace visualizer.Models;

public class Animation(IReadOnlyList<Action> steps)
{
    private int _currentStepIndex;
    private IReadOnlyList<Action> Steps { get; } = steps;
    public Action ResetStep { get; set; } = () => {};
    
    public int CurrentStepIndex => _currentStepIndex;
    public int StepCount => Steps.Count;
    public bool CanStepForward => _currentStepIndex < Steps.Count;
    public bool CanStepBackward => _currentStepIndex > 0;
    public bool IsComplete => _currentStepIndex >= Steps.Count;

    public bool NextStep() => TryStepForward();

    public bool TryStepForward()
    {
        if (!CanStepForward) return false;
        Steps[_currentStepIndex]();
        _currentStepIndex++;
        return true;
    }

    public void ReplayTo(int targetStepIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(targetStepIndex);

        if (targetStepIndex > Steps.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStepIndex));
        }

        while (_currentStepIndex < targetStepIndex && TryStepForward())
        {
        }
    }

    public void Reset()
    {
        _currentStepIndex = 0;
        ResetStep();
    }
}