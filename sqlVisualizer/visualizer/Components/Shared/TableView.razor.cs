using Microsoft.AspNetCore.Components;
using visualizer.Models;

namespace visualizer.Components.Shared;

public class TableViewBase : ComponentBase
{
    [Parameter] public required Table Table { get; init; }
}