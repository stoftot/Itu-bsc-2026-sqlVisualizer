using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class Footer : ComponentBase
{
    [Inject] HomeState HomeState { get; set; }
    [Inject] IJSRuntime JS { get; set; }
    
    private async Task CopySessionIdToClipboard()
    {
        if (HomeState?.SessionId != null)
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", HomeState.SessionId);
        }
    }
}