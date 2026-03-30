using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class Header : ComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }

    private void ToggleSidebar()
    {
        Console.WriteLine("Toggling sidebar");
        HomeState.ViewSidebar = !HomeState.ViewSidebar;
        HomeState.NotifyStateChanged();
    }
}