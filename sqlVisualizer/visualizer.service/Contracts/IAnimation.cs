using commonDataModels.Models;

namespace visualizer.service.Contracts;

/// <summary>
/// Represents one SQL step as a replayable animation over display tables.
/// </summary>
public interface IAnimation
{
    /// <summary>
    /// Gets the input tables for the step.
    /// </summary>
    public IReadOnlyList<IDisplayTable> FromTables();

    /// <summary>
    /// Gets the output tables for the step.
    /// </summary>
    public IReadOnlyList<IDisplayTable> ToTables();

    /// <summary>
    /// Gets the SQL keyword represented by this animation.
    /// </summary>
    public SQLKeyword Keyword();

    /// <summary>
    /// Gets the number of animation actions in the step.
    /// </summary>
    public int NumberOfAnimationSteps();

    /// <summary>
    /// Gets the current animation cursor.
    /// </summary>
    public int StepIndex();

    /// <summary>
    /// Gets the total number of animation actions in the step.
    /// </summary>
    public int StepCount();

    /// <summary>
    /// Gets whether the animation has reached its final action.
    /// </summary>
    public bool IsComplete();

    /// <summary>
    /// Gets whether the animation can move forward one action.
    /// </summary>
    public bool CanStepForward();

    /// <summary>
    /// Gets whether the animation can be replayed backward.
    /// </summary>
    public bool CanStepBackward();

    /// <summary>
    /// Applies the next animation action if available.
    /// </summary>
    public bool TryStepForward();

    /// <summary>
    /// Resets the animation and replays it up to the requested action index.
    /// </summary>
    public void ReplayTo(int targetStepIndex);

    /// <summary>
    /// Resets the animation back to its initial visual state.
    /// </summary>
    public void Reset();
}
