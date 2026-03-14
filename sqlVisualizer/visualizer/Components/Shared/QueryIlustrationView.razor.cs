using Microsoft.AspNetCore.Components;
using visualizer.Models;
using visualizer.Repositories;

namespace visualizer.Components.Shared;

public class QueryIllustrationViewBase : ComponentBase, IDisposable
{
    private const int AnimationDelayMs = 1100;

    [Parameter] public required string Query { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] private MetricsConfig MetricsConfig { get; init; } = null!;
    [Inject] public required VisualisationsGenerator VisualisationsGenerator { get; init; }
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    public List<Table> FromTables { get; set; } = [];
    public List<Table> ToTables { get; set; } = [];
    private List<Visualisation> Steps { get; set; } = [];
    private int _indexOfStepToHighlight;
    private CancellationTokenSource? _animationCancellationTokenSource;
    private Task? _animationPlaybackTask;
    private bool _animationMetricsRunning;

    private int IndexOfStepToHighlight => _indexOfStepToHighlight;
    private Visualisation CurrStep => Steps[IndexOfStepToHighlight];
    protected bool ShowAggregation => FromTables.Count != 0 && FromTables[0].Aggregations.Count != 0;

    protected override void OnInitialized()
    {
        HomeState.NextStep = OnNextStep;
        HomeState.PreviousStep = OnPreviousStep;
        HomeState.AnimatePlay = OnAnimatePlay;
        HomeState.AnimatePause = OnAnimatePause;
        HomeState.AnimateStepNext = OnAnimateStepNext;
        HomeState.AnimateStepPrivious = OnAnimateStepPrevious;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Init();
    }

    public async Task Init()
    {
        await CancelAnimationPlaybackAsync();
        await ReloadVisualisationsAsync(stepIndex: 0, animationStepIndex: 0, trackMetrics: true);
    }

    private async Task ReloadVisualisationsAsync(int stepIndex, int animationStepIndex, bool trackMetrics)
    {
        Steps = VisualisationsGenerator.Generate(Query);
        HomeState.Steps = Steps;

        if (Steps.Count == 0)
        {
            _indexOfStepToHighlight = 0;
            HomeState.CurrentStepIndex = 0;
            FromTables = [];
            ToTables = [];
            RefreshAnimationState();
            await InvokeAsync(StateHasChanged);
            return;
        }

        _indexOfStepToHighlight = Math.Clamp(stepIndex, 0, Steps.Count - 1);
        HomeState.CurrentStepIndex = _indexOfStepToHighlight;

        var animation = CurrStep.Animation;
        animation.ReplayTo(Math.Clamp(animationStepIndex, 0, animation.StepCount));

        if (trackMetrics)
        {
            MetricsHandler.EnterStep(HomeState.SessionId, CurrStep.Component.Keyword);
            MetricsHandler.PrintSessionTimings(HomeState.SessionId);
        }

        UpdateStepShown();
        RefreshAnimationState();
        await InvokeAsync(StateHasChanged);
    }

    private void UpdateStepShown()
    {
        if (Steps.Count == 0)
        {
            FromTables = [];
            ToTables = [];
            return;
        }

        FromTables = CurrStep.FromTables;
        ToTables = CurrStep.ToTables;
    }

    private void RefreshAnimationState()
    {
        if (Steps.Count == 0)
        {
            HomeState.IsAnimationPlaying = false;
            HomeState.CurrentAnimationStepIndex = 0;
            HomeState.CurrentAnimationStepCount = 0;
            HomeState.NotifyStateChanged();
            return;
        }

        var animation = CurrStep.Animation;
        HomeState.CurrentAnimationStepIndex = animation.CurrentStepIndex;
        HomeState.CurrentAnimationStepCount = animation.StepCount;
        HomeState.NotifyStateChanged();
    }

    private async Task PlayAnimationAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && CurrStep.Animation.TryStepForward())
            {
                UpdateStepShown();
                RefreshAnimationState();
                await InvokeAsync(StateHasChanged);
                await Task.Delay(AnimationDelayMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            HomeState.IsAnimationPlaying = false;
            StopAnimationMetricsIfRunning();
            RefreshAnimationState();
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task StartAnimationPlaybackAsync()
    {
        if (Steps.Count == 0 || HomeState.IsAnimationPlaying || !CurrStep.Animation.CanStepForward)
        {
            RefreshAnimationState();
            return Task.CompletedTask;
        }

        _animationCancellationTokenSource?.Dispose();
        _animationCancellationTokenSource = new CancellationTokenSource();

        HomeState.IsAnimationPlaying = true;
        StartAnimationMetricsIfNeeded();
        RefreshAnimationState();
        _animationPlaybackTask = PlayAnimationAsync(_animationCancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    private async Task CancelAnimationPlaybackAsync()
    {
        if (_animationPlaybackTask is null)
        {
            HomeState.IsAnimationPlaying = false;
            StopAnimationMetricsIfRunning();
            RefreshAnimationState();
            return;
        }

        _animationCancellationTokenSource?.Cancel();

        try
        {
            await _animationPlaybackTask;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _animationCancellationTokenSource?.Dispose();
            _animationCancellationTokenSource = null;
            _animationPlaybackTask = null;
        }
    }

    private void StartAnimationMetricsIfNeeded()
    {
        if (_animationMetricsRunning) return;
        MetricsHandler.StartAnimation(HomeState.SessionId);
        _animationMetricsRunning = true;
    }

    private void StopAnimationMetricsIfRunning()
    {
        if (!_animationMetricsRunning) return;
        MetricsHandler.StopAnimation(HomeState.SessionId);
        _animationMetricsRunning = false;
    }

    private async Task OnAnimatePlay()
    {
        MetricsConfig.AnimateButtonClicks.Add(1);

        if (Steps.Count == 0) return;

        if (CurrStep.Animation.IsComplete)
        {
            await ReloadVisualisationsAsync(IndexOfStepToHighlight, animationStepIndex: 0, trackMetrics: false);
        }

        await StartAnimationPlaybackAsync();
    }

    private async Task OnAnimatePause()
    {
        await CancelAnimationPlaybackAsync();
    }

    private async Task OnAnimateStepNext()
    {
        await CancelAnimationPlaybackAsync();

        if (Steps.Count == 0 || !CurrStep.Animation.TryStepForward())
        {
            RefreshAnimationState();
            await InvokeAsync(StateHasChanged);
            return;
        }

        UpdateStepShown();
        RefreshAnimationState();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnAnimateStepPrevious()
    {
        await CancelAnimationPlaybackAsync();

        if (Steps.Count == 0 || !CurrStep.Animation.CanStepBackward)
        {
            RefreshAnimationState();
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ReloadVisualisationsAsync(IndexOfStepToHighlight,
            animationStepIndex: CurrStep.Animation.CurrentStepIndex - 1,
            trackMetrics: false);
    }

    private async Task OnNextStep()
    {
        MetricsConfig.NextButtonClicks.Add(1);

        if (IndexOfStepToHighlight >= Steps.Count - 1) return;

        await CancelAnimationPlaybackAsync();
        await ReloadVisualisationsAsync(IndexOfStepToHighlight + 1, animationStepIndex: 0, trackMetrics: true);
    }

    private async Task OnPreviousStep()
    {
        MetricsConfig.PrevButtonClicks.Add(1);

        if (IndexOfStepToHighlight <= 0) return;

        await CancelAnimationPlaybackAsync();
        await ReloadVisualisationsAsync(IndexOfStepToHighlight - 1, animationStepIndex: 0, trackMetrics: true);
    }

    public void Dispose()
    {
        _animationCancellationTokenSource?.Cancel();
        _animationCancellationTokenSource?.Dispose();
        StopAnimationMetricsIfRunning();
    }
}