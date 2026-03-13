using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using visualizer.Models;

namespace visualizer.Components.Shared;

public class TableViewBase : ComponentBase
{
    [Parameter] public required Table Table { get; init; }
    
    [Inject] IJSRuntime JS { get; init; }
    
    protected ElementReference tableWrapper;
    protected double tableHeight = 10;
    protected double braceHeight => tableHeight; // smaller than table

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var newHeight = await JS.InvokeAsync<double>("getElementHeight", tableWrapper);

        if (Math.Abs(newHeight - tableHeight) > 0.5)
        {
            tableHeight = newHeight;
            StateHasChanged();
        }
    }

    protected string BracePath(double h)
    {
        var mid = h / 2.0;
        return
            $"M 28 0 " +
            $"C 8 0, 8 {h * 0.18}, 18 {mid * 0.75} " +
            $"C 22 {mid * 0.88}, 22 {mid * 0.96}, 10 {mid} " +
            $"C 22 {mid * 1.04}, 22 {mid * 1.12}, 18 {h - (mid * 0.75)} " +
            $"C 8 {h * 0.82}, 8 {h}, 28 {h}";
    }
}