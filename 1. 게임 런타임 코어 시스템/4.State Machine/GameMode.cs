using System;
using System.Threading;
using UnityEngine;

public abstract class GameMode<TState> where TState : Enum
{
    private readonly Action<TState> changeModeAction;
    private IGameModeEvent modeEvent;
    private TState requestedState;
    private bool hasRequestedState;

    protected GameMode(Action<TState> changeModeAction)
    {
        this.changeModeAction = changeModeAction;
    }

    public void connectEvent(IGameModeEvent gameModeEvent)
    {
        modeEvent = gameModeEvent;
    }

    public async Awaitable update(CancellationToken cancellationToken)
    {
        hasRequestedState = false;
        await enter(cancellationToken);
        while (!hasRequestedState && !cancellationToken.IsCancellationRequested)
        {
            evaluateTransition();
            await Awaitable.NextFrameAsync(cancellationToken);
        }
        if (cancellationToken.IsCancellationRequested) return;
        await exit(cancellationToken);
        doNextMode();
        await Awaitable.NextFrameAsync(cancellationToken);
    }

    public void changeMode(TState nextState)
    {
        requestedState = nextState;
        hasRequestedState = true;
    }
    
    protected abstract void evaluateTransition();

    private void doNextMode()
    {
        if (!hasRequestedState)
        {
            return;
        }

        changeModeAction?.Invoke(requestedState);
    }

    public async Awaitable enter(CancellationToken cancellationToken)
    {
        if (modeEvent == null) return;

        await modeEvent.invokeEnter(cancellationToken);
    }

    public async Awaitable exit(CancellationToken cancellationToken)
    {
        if (modeEvent == null) return;

        await modeEvent.invokeExit(cancellationToken);
    }
}
