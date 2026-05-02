using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using visualizer.Models;

namespace visualizer.Components.Shared;

public class TableViewBase : ComponentBase
{
    [Parameter] public required IDisplayTable? Table { get; init; }

    protected bool ShowAggregation => Table != null && Table.Aggregations().Count != 0;
}