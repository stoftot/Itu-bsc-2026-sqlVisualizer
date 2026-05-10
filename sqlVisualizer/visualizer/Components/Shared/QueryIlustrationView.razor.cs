using Microsoft.AspNetCore.Components;
using visualizer.service;
using visualizer.service.Contracts;
using visualizer.service.Exceptions;
using visualizer.service.Repositories;

namespace visualizer.Components.Shared;

public class QueryIllustrationViewBase : ComponentBase, IDisposable
{
    private const int AnimationDelayMs = 1100;

    [Parameter] public required string Query { get; init; }
    [Inject] public required HomeState HomeState { get; init; }
    [Inject] private MetricsConfig MetricsConfig { get; init; } = null!;
    [Inject] public required IMetricsHandler MetricsHandler { get; init; }
    [Inject] public required IAnimationGenerator AnimationGenerator { get; init; }
    
    public IReadOnlyList<IDisplayTable> FromTables { get; set; } = [];
    public IReadOnlyList<IDisplayTable> ToTables { get; set; } = [];
    private IReadOnlyList<IAnimation> Steps { get; set; } = [];
    private int _indexOfStepToHighlight;
    private CancellationTokenSource? _animationCancellationTokenSource;
    private Task? _animationPlaybackTask;
    private bool _animationMetricsRunning;

    private int IndexOfStepToHighlight => _indexOfStepToHighlight;
    private IAnimation CurrStep => Steps[IndexOfStepToHighlight];
    protected bool ShowAggregation => FromTables.Count != 0 && FromTables[0].Aggregations().Count != 0;

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
            Steps = AnimationGenerator.Generate(Query);
        }
        catch (SQLParseException e)
        {
            await HandleException(e, e.Message);
            return;
        }
        catch (NotImplementedException e)
        {
            await HandleException(e, "This is still a new tool, " +
                                     "and it seems that what you are trying to do hasn't been implemented yet.");
            return;
        }
        catch (Exception e)
        {
            await HandleException(e, "An internal error occured, sorry for the inconvenience");
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


    private async Task HandleException(Exception e, string messageToUser)
    {
        Console.WriteLine(e.ToString());
        HomeState.ExceptionOccured = true;
        HomeState.ExceptionMessage = messageToUser;
        Steps = [];
        HomeState.NotifyStateChanged();
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task SelectStepAsync(int stepIndex, bool trackMetrics)
    {
        if(Steps.Count == 0) return;
        
        _indexOfStepToHighlight = Math.Clamp(stepIndex, 0, Steps.Count - 1);
        HomeState.CurrentStepIndex = _indexOfStepToHighlight;
        ResetCurrentAnimation();

        if (trackMetrics)
        {
            MetricsHandler.EnterStep(HomeState.SessionId, CurrStep.Keyword());
        }

        await RefreshCurrentViewAsync();
    }

    private void ResetCurrentAnimation()
    {
        CurrStep.Reset();
    }

    private void ReplayCurrentAnimationTo(int animationStepIndex)
    {
        ResetCurrentAnimation();
        CurrStep.ReplayTo(Math.Clamp(animationStepIndex, 0, CurrStep.NumberOfAnimationSteps()));
    }

    private void UpdateStepShown()
    {
        if (Steps.Count == 0)
        {
            FromTables = [];
            ToTables = [];
            return;
        }

        FromTables = CurrStep.FromTables();
        ToTables = CurrStep.ToTables();
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

        var animation = CurrStep;
        HomeState.CurrentAnimationStepIndex = animation.StepIndex();
        HomeState.CurrentAnimationStepCount = animation.StepCount();
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
            while (!cancellationToken.IsCancellationRequested && CurrStep.TryStepForward())
            {
                UpdateStepShown();
                RefreshAnimationState();
                await InvokeAsync(StateHasChanged);
                await Task.Delay(AnimationDelayMs, cancellationToken);
            }
            // Animation completed naturally
            if (_animationMetricsRunning && CurrStep.IsComplete())
            {
                RecordAnimationViewPercentage(100);
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
        if (Steps.Count == 0 || HomeState.IsAnimationPlaying || !CurrStep.CanStepForward())
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
        return CurrStep.StepCount() == 0 ? 0 : (double)CurrStep.StepIndex() / CurrStep.StepCount() * 100;
    }
    
    private void RecordAnimationViewPercentage(double percentage)
    {
        if (Steps.Count == 0) return;
        MetricsHandler.RecordAnimationViewPercentage(HomeState.SessionId, CurrStep.Keyword(), percentage);
    }

    private void StopAnimationMetricsIfRunning()
    {
        if (!_animationMetricsRunning) return;
        MetricsHandler.StopAnimation(HomeState.SessionId);
        MetricsHandler.EnterStep(HomeState.SessionId, CurrStep.Keyword());
        _animationMetricsRunning = false;
    }

    private void TryRecordAnimationViewPercentage()
    {
        if (Steps.Count == 0 || IndexOfStepToHighlight >= Steps.Count) return;

        if (CurrStep.StepIndex() == 0) return;
        var percentage = GetCurrentAnimationPercentage();
        RecordAnimationViewPercentage(percentage);
    }

    private async Task OnAnimatePlay()
    {

        if (Steps.Count == 0) return;

        if (CurrStep.IsComplete())
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

        CurrStep.TryStepForward();
        
        if (CurrStep.IsComplete())
        {
            RecordAnimationViewPercentage(100);
        }
        
        await RefreshCurrentViewAsync();

    }

    private async Task OnAnimateStepPrevious()
    {
        await CancelAnimationPlaybackAsync();

        if (Steps.Count == 0 || !CurrStep.CanStepBackward())
        {
            RefreshAnimationState();
            await InvokeAsync(StateHasChanged);
            return;
        }

        ReplayCurrentAnimationTo(CurrStep.StepIndex() - 1);
        await RefreshCurrentViewAsync();
    }
    
    private async Task OnSelectStep(int stepIndex)
    {
        TryRecordAnimationViewPercentage();
        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(stepIndex, trackMetrics: true);
    }

    private async Task OnNextStep()
    {
        if (IndexOfStepToHighlight >= Steps.Count - 1) return;

        TryRecordAnimationViewPercentage();
        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(IndexOfStepToHighlight + 1, trackMetrics: true);
    }

    private async Task OnPreviousStep()
    {
        if (IndexOfStepToHighlight <= 0) return;

        TryRecordAnimationViewPercentage();
        await CancelAnimationPlaybackAsync();
        await SelectStepAsync(IndexOfStepToHighlight - 1, trackMetrics: true);
    }

    public void Dispose()
    {
        _animationCancellationTokenSource?.Cancel();
        _animationCancellationTokenSource?.Dispose();
    }
}
