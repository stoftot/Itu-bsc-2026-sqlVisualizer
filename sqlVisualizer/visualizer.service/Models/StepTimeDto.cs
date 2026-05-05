namespace visualizer.Models;

public class StepTimeDto(string step, long timeSpentMs, long animationMs)
{
   public string Step { get; set; } = step;
   public long TimeSpentMs { get; set; } = timeSpentMs;
   public long AnimationMs { get; set; } = animationMs;
}