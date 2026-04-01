using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using visualizer.Exstensions;
using visualizer.Models;
using visualizer.Repositories.AnimationClasses;

namespace visualizer.Repositories;

public static class AnimationGenerator
{
    private static TableVisualModifier tvm = new();
    public static Animation Generate(List<Table> fromTables, List<Table> toTables, SQLDecompositionComponent action)
    {
        var animation = action.Keyword switch
        {
            SQLKeyword.FROM => throw new NotImplementedException("FROM animations are not yet supported"),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN => JoinAnimationGenerator.Generate(fromTables, toTables[0], action),
            SQLKeyword.LEFT_JOIN => throw new NotImplementedException("LEFT JOIN animations are not yet supported"),
            SQLKeyword.RIGHT_JOIN => throw new NotImplementedException("RIGHT JOIN animations are not yet supported"),
            SQLKeyword.FULL_JOIN => throw new NotImplementedException("FULL JOIN animations are not yet supported"),
            SQLKeyword.WHERE => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("WHERE animation can only be generated from one table to another")
                : WhereAnimationGenerator.Generate(fromTables[0], toTables[0], action),
            SQLKeyword.GROUP_BY =>
                fromTables.Count > 1
                    ? throw new ArgumentException("GROUP BY animations can only be generated from one table")
                    : GroupByAnimationGenerator.Generate(fromTables[0], toTables, action),
            SQLKeyword.HAVING => HavingAnimationGenerator.Generate(fromTables, toTables, action),
            SQLKeyword.SELECT =>
                toTables.Count > 1
                    ? throw new ArgumentException("SELECT animations can only be generated to one table")
                    : SelectAnimationGenerator.Generate(fromTables, toTables[0], action),
            SQLKeyword.ORDER_BY => throw new NotImplementedException("ORDER BY animations are not yet supported"),
            SQLKeyword.LIMIT => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("LIMIT animation can only be generated from one table to another")
                : LimitAnimationGenerator.Generate(fromTables[0], toTables[0], action),
            SQLKeyword.OFFSET => throw new NotImplementedException("OFFSET animations are not yet supported"),
            _ => throw new ArgumentOutOfRangeException()
        };

        animation.ResetStep =
            tvm.CombineActions(
            [
                tvm.ResetTables(fromTables.ToList()),
                tvm.ResetTables(toTables.ToList())
            ]);
        
        return animation;
    }
}
