using Microsoft.AspNetCore.Components;
using visualizer.service.Repositories;

namespace visualizer.Components.Shared;

public partial class ErrorPopup : ComponentBase
{
    [Inject] public required HomeState HomeState { get; init; }
     public void ClosePopup()
     {
         HomeState.ExceptionOccured = false;
         HomeState.ExceptionMessage = string.Empty;
         StateHasChanged();
     }
     
     protected override void OnInitialized()
     {
         HomeState.StateChanged += StateHasChanged;
     }

     public void Dispose()
     {
         HomeState.StateChanged -= StateHasChanged;
     }
}