namespace visualizer.service.Contracts;

public interface IAnimationGenerator
{
    public IReadOnlyList<IAnimation> Generate(string sql);
}