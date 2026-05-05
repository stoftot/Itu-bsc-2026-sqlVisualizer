using visualizer.Models;

namespace visualizer.Contracts;

public interface IAnimationGenerator
{
    public IReadOnlyList<IAnimation> Generate(string sql);
}