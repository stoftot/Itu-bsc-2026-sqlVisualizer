using animationGeneration.AnimationClasses;
using animationGeneration.Contracts;
using animationGeneration.Extensions;
using animationGeneration.Models;
using commonDataModels.Models;
using visualizer.service.Contracts;

namespace animationGeneration;

internal class AnimationGenerator(ITablesPerExecutionStepGenerator generator) : IAnimationGenerator
{
    private static TableVisualModifier tvm = new();
    
    public IReadOnlyList<IAnimation> Generate(string sql)
    {
        var executedSteps = generator.Generate(sql);
        return executedSteps.Select(executedStep => 
            GenerateAnimation(executedStep.FromTables().ToDisplay(), 
                executedStep.ToTables().ToDisplay(), 
                executedStep.SQLComponent()))
            .ToList();
    }
    
    private Animation GenerateAnimation(List<DisplayTable> fromTables, List<DisplayTable> toTables, ISQLComponent sql)
    {
        var animationSteps = sql.Keyword() switch
        {
            SQLKeyword.FROM => throw new NotImplementedException("FROM animations are not yet supported"),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN or 
                SQLKeyword.LEFT_JOIN or SQLKeyword.LEFT_OUTER_JOIN or 
                SQLKeyword.RIGHT_JOIN or SQLKeyword.RIGHT_OUTER_JOIN or 
                SQLKeyword.FULL_JOIN or SQLKeyword.FULL_OUTER_JOIN 
                => fromTables.Count != 2
                ?  throw new ArgumentException("JOIN animations can only be generated from two tables to one")
                : JoinAnimationGenerator.Generate(fromTables, toTables[0], sql),
            SQLKeyword.WHERE => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("WHERE animation can only be generated from one table to another")
                : WhereAnimationGenerator.Generate(fromTables[0], toTables[0], sql),
            SQLKeyword.GROUP_BY =>
                fromTables.Count > 1
                    ? throw new ArgumentException("GROUP BY animations can only be generated from one table")
                    : GroupByAnimationGenerator.Generate(fromTables[0], toTables, sql),
            SQLKeyword.HAVING => HavingAnimationGenerator.Generate(fromTables, toTables),
            SQLKeyword.SELECT =>
                toTables.Count > 1
                    ? throw new ArgumentException("SELECT animations can only be generated to one table")
                    : SelectAnimationGenerator.Generate(fromTables, toTables[0], sql),
            SQLKeyword.ORDER_BY => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("LIMIT animation can only be generated from one table to another")
                : OrderByAnimationGenerator.Generate(fromTables[0], toTables[0], sql),
            SQLKeyword.LIMIT => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("LIMIT animation can only be generated from one table to another")
                : LimitAnimationGenerator.Generate(fromTables[0], toTables[0], sql),
            SQLKeyword.OFFSET => throw new NotImplementedException("OFFSET animations are not yet supported"),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return new Animation(animationSteps, sql.Keyword(), fromTables, toTables)
        {
            ResetStep = tvm.CombineActions(
            [
                tvm.ResetTables(fromTables.ToList()),
                tvm.ResetTables(toTables.ToList())
            ])
        };
    }
}
