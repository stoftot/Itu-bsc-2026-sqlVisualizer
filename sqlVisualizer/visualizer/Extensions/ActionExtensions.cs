namespace visualizer.Exstensions;

public static class ActionExtensions
{
    public static Action ToOneAction(this IEnumerable<Action> actions)
    {
        //capture the list, so when its changed it doesn't apply to all functions
        var snapshot = actions.ToList();
        return () =>
        {
            foreach (var action in snapshot) action();
        };
    }
}