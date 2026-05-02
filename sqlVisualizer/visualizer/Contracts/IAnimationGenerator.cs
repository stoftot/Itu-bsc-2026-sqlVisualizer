using visualizer.Models;

namespace visualizer.Contracts;

public interface IAnimationGenerator
{
    public IList<IAnimation> Generate(string sql);
}