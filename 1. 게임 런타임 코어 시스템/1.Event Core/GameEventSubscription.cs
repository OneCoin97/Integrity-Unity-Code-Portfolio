using System;
using System.Threading;
using UnityEngine;

public sealed class GameEventSubscription : IComparable<GameEventSubscription>
{
    public readonly Action func;
    public readonly Func<CancellationToken, Awaitable> funcAsync;
    public readonly int priority;
    public readonly int sequence;
    public readonly bool once;

    public GameEventSubscription(Action func, int priority, int sequence, bool once = false)
    {
        this.func = func;
        this.priority = priority;
        this.sequence = sequence;
        this.once = once;
    }

    public GameEventSubscription(Func<CancellationToken, Awaitable> funcAsync, int priority, int sequence, bool once = false)
    {
        this.funcAsync = funcAsync;
        this.priority = priority;
        this.sequence = sequence;
        this.once = once;
    }

    public int CompareTo(GameEventSubscription other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        int priorityComparison = priority.CompareTo(other.priority);
        if (priorityComparison != 0) return priorityComparison;

        return sequence.CompareTo(other.sequence);
    }
}
