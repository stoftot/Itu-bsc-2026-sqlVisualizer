using Microsoft.AspNetCore.Components;

namespace visualizer.Components.Shared;

public partial class InfoModal : ComponentBase
{
    public required bool ShowModal = false;
    public void ToggleModal()
    {
        ShowModal = !ShowModal;
        StateHasChanged();
    }
}