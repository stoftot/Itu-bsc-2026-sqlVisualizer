using Microsoft.AspNetCore.Components;
using visualizer.service.Repositories;

namespace visualizer.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }
}