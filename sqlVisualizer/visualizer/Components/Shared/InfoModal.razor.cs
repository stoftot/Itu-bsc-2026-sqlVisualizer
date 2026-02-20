using Microsoft.AspNetCore.Components;

namespace visualizer.Components.Shared;

public partial class InfoModal : ComponentBase
{
    public required bool ShowModal = true;
    public void ToggleModal()
    {
        ShowModal = !ShowModal;
        StateHasChanged();
    }
}