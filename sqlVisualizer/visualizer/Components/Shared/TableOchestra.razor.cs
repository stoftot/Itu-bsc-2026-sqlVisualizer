using Microsoft.AspNetCore.Components;
using visualizer.Models;

namespace visualizer.Components.Shared;

public class TableOchestraBase : ComponentBase
{
    [Parameter] public required List<Table> Tables { get; init; }
}