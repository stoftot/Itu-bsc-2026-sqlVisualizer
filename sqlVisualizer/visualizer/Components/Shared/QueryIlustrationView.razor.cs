using Microsoft.AspNetCore.Components;
using visualizer.Models;

namespace visualizer.Components.Shared;

public class QueryIlustrationViewBase : ComponentBase
{
    [Parameter] public required List<Table> FromTables { get; init; }
    [Parameter] public required Table ToTable { get; init; }
}