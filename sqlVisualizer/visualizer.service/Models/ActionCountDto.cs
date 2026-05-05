namespace visualizer.Models;

public class ActionCountDto(string actionName, long count)
{
    public string ActionName { get; set; } = actionName;
    public long Count { get; set; } = count;
}