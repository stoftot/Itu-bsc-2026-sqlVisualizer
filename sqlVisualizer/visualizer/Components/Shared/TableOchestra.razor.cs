using Microsoft.AspNetCore.Components;
using visualizer.service.Contracts;

namespace visualizer.Components.Shared;

public class TableOchestraBase : ComponentBase
{
    [Parameter] public required IReadOnlyList<IDisplayTable> Tables { get; init; }
}