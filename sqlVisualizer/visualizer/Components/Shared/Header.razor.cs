using Microsoft.AspNetCore.Components;
using visualizer.service.Repositories;

namespace visualizer.Components.Shared;

public partial class Header : ComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }

    private void ToggleSidebar()
    {
        HomeState.ViewSidebar = !HomeState.ViewSidebar;
        HomeState.NotifyStateChanged();
    }
}