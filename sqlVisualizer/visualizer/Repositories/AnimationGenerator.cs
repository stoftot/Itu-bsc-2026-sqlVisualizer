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
        return action.Keyword switch
        {
            SQLKeyword.FROM => throw new NotImplementedException(),
            SQLKeyword.JOIN or SQLKeyword.INNER_JOIN or 
                SQLKeyword.LEFT_JOIN or SQLKeyword.LEFT_OUTER_JOIN or 
                SQLKeyword.RIGHT_JOIN or SQLKeyword.RIGHT_OUTER_JOIN or 
                SQLKeyword.FULL_JOIN or SQLKeyword.FULL_OUTER_JOIN 
                => JoinAnimationGenerator.Generate(fromTables, toTables[0], action),
            SQLKeyword.WHERE => fromTables.Count > 1 && toTables.Count > 1
                ? throw new ArgumentException("where animation can only be generated from one table to another")
                : WhereAnimationGenerator.Generate(fromTables[0], toTables[0], action),
            SQLKeyword.GROUP_BY =>
                fromTables.Count > 1
                    ? throw new ArgumentException("group by animations can only be generated from one table")
                    : GroupByAnimationGenerator.Generate(fromTables[0], toTables, action),
            SQLKeyword.HAVING => HavingAnimationGenerator.Generate(fromTables, toTables, action),
            SQLKeyword.SELECT =>
                toTables.Count > 1
                    ? throw new ArgumentException("select animations can only be generated to one table")
                    : SelectAnimationGenerator.Generate(fromTables, toTables[0], action),
            SQLKeyword.ORDER_BY => throw new NotImplementedException(),
            SQLKeyword.LIMIT => throw new NotImplementedException(),
            SQLKeyword.OFFSET => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}