namespace visualizer.Exstension_methods;

public static class IReadOnlyListExstension
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (Equals(list[i], target))
                return i;
        }

        return -1;
    }
}