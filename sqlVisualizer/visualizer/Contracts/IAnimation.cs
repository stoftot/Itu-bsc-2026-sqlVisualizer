namespace visualizer.Models;

public interface IAnimation
{
    public IReadOnlyList<IDisplayTable> FromTables();
    public IReadOnlyList<IDisplayTable> ToTables();
    public SQLKeyword Keyword();
    public int NumberOfAnimationSteps();
    public int StepIndex();
    public int StepCount();
    
    public bool IsComplete();
    public bool CanStepForward();
    public bool CanStepBackward();
    public bool TryStepForward();
    public void ReplayTo(int targetStepIndex);
    public void Reset();
}