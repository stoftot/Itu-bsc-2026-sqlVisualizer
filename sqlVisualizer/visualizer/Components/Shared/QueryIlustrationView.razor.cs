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
        HomeState.AnimateStepPrevious = OnAnimateStepPrevious;
        HomeState.SelectStep = OnSelectStep;
    }
    protected override async Task OnParametersSetAsync()
    {
        TryRecordAnimationViewPercentage();
        await base.OnParametersSetAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Init();
    }

    public async Task Init()
    {
        await CancelAnimationPlaybackAsync();
        try
        {
            Steps = VisualisationsGenerator.Generate(Query);
        }
        catch (Exception e)
        {
            HomeState.ExceptionOccured = true;
            HomeState.ExceptionMessage = e.Message;
            Steps = [];
            HomeState.NotifyStateChanged();
            await InvokeAsync(StateHasChanged);
            return;
        }
        
        if (Steps.Count == 0)
        {
            _indexOfStepToHighlight = 0;
            HomeState.CurrentStepIndex = 0;
            FromTables = [];
            ToTables = [];
            await RefreshCurrentViewAsync();
            return;
        }

        HomeState.Steps = Steps;
        await SelectStepAsync(stepIndex: 0, trackMetrics: true);
    }

    private async Task SelectStepAsync(int stepIndex, bool trackMetrics)
    {
        TryRecordAnimationViewPercentage();
        if(Steps.Count == 0) return;
        
        _indexOfStepToHighlight = Math.Clamp(stepIndex, 0, Steps.Count - 1);
        HomeState.CurrentStepIndex = _indexOfStepToHighlight;
        ResetCurrentAnimation();

        if (trackMetrics)
        {
            MetricsHandler.EnterStep(HomeState.SessionId, CurrStep.Component.Keyword);
        }

        await RefreshCurrentViewAsync();
    }

    private void ResetCurrentAnimation()
    {
        CurrStep.Animation.Reset();
    }

    private void ReplayCurrentAnimationTo(int animationStepIndex)
    {
        ResetCurrentAnimation();
        CurrStep.Animation.ReplayTo(Math.Clamp(animationStepIndex, 0, CurrStep.Animation.StepCount));
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

    private async Task RefreshCurrentViewAsync()
    {
        UpdateStepShown();
        RefreshAnimationState();
        await InvokeAsync(StateHasChanged);
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
            // Animation completed naturally
            if (_animationMetricsRunning && CurrStep.Animation.IsComplete)
            {
                RecordAnimationViewPercentage(100);
                _animationMetricsRunning = false;
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

    private double GetCurrentAnimationPercentage()
    {
        if (Steps.Count == 0) return 0;
        return CurrStep.Animation.StepCount == 0 ? 0 : (double)CurrStep.Animation.CurrentStepIndex / CurrStep.Animation.StepCount * 100;
    }
    
    private void RecordAnimationViewPercentage(double percentage)
    {
        if (Steps.Count == 0) return;
        MetricsHandler.RecordAnimationViewPercentage(HomeState.SessionId, CurrStep.Component.Keyword, percentage);
    }

    private void StopAnimationMetricsIfRunning()
    {
        if (!_animationMetricsRunning) return;
        MetricsHandler.EnterStep(HomeState.SessionId, CurrStep.Component.Keyword);
        MetricsHandler.StopAnimation(HomeState.SessionId);
        _animationMetricsRunning = false;
    }

    private void TryRecordAnimationViewPercentage()
    {
        if (Steps.Count == 0) return;

        //if (CurrStep.Animation.CurrentStepIndex == 0) return;
        var percentage = GetCurrentAnimationPercentage();
        RecordAnimationViewPercentage(percentage);
    }

    private async Task OnAnimatePlay()
    {

        if (Steps.Count == 0) return;

        if (CurrStep.Animation.IsComplete)
        {
            ResetCurrentAnimation();
            await RefreshCurrentViewAsync();
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

        CurrStep.Animation.TryStepForward();
        
        if (CurrStep.Animation.IsComplete)
        {
            RecordAnimationViewPercentage(100);
        }
        
        await RefreshCurrentViewAsync();

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

        ReplayCurrentAnimationTo(CurrStep.Animation.CurrentStepIndex - 1);
        await RefreshCurrentViewAsync();
    }
    
    private async Task OnSelectStep(int stepIndex)
    {
        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(stepIndex, trackMetrics: true);
    }

    private async Task OnNextStep()
    {
        if (IndexOfStepToHighlight >= Steps.Count - 1) return;

        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(IndexOfStepToHighlight + 1, trackMetrics: true);
    }

    private async Task OnPreviousStep()
    {
        if (IndexOfStepToHighlight <= 0) return;

        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(IndexOfStepToHighlight - 1, trackMetrics: true);
    }

    public void Dispose()
    {
        _animationCancellationTokenSource?.Cancel();
        _animationCancellationTokenSource?.Dispose();
    }
}