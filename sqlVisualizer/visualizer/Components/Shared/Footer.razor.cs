using Microsoft.AspNetCore.Components;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public partial class Footer : ComponentBase
{
    [Inject] HomeState HomeState { get; set; }
    
    
}