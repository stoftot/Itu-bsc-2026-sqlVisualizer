namespace visualizer.Utility;

public interface IDatabase
{
    public string Name();
    public IList<ISimpleTable> Tables();
}