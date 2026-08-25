using System;
using System.Threading;
using UnityEngine;

public interface IGameModeEvent
{
    Awaitable invokeEnter(CancellationToken cancellationToken);
    Awaitable invokeExit(CancellationToken cancellationToken);
}

public sealed class GameModeEvent : IGameModeEvent
{
    private readonly GameEventSubscriptions enterSubscriptions = new();
    private readonly GameEventSubscriptions exitSubscriptions = new();

    public async Awaitable invokeEnter(CancellationToken cancellationToken)
    {
        await enterSubscriptions.invoke(cancellationToken);
    }

    public async Awaitable invokeExit(CancellationToken cancellationToken)
    {
        await exitSubscriptions.invoke(cancellationToken);
    }

    public void addFunction(Action action, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        (enter ? enterSubscriptions : exitSubscriptions).addFunction(action, priority, sequence, once);
    }

    public void addFunction(Func<CancellationToken, Awaitable> function, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        (enter ? enterSubscriptions : exitSubscriptions).addFunction(function, priority, sequence, once);
    }

    public void removeFunction(Action action, bool enter)
    {
        (enter ? enterSubscriptions : exitSubscriptions).removeFunction(action);
    }

    public void removeFunction(Func<CancellationToken, Awaitable> function, bool enter)
    {
        (enter ? enterSubscriptions : exitSubscriptions).removeFunction(function);
    }
}
