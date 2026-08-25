using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public sealed class GameEventSubscriptions
{
    private readonly List<GameEventSubscription> functions = new();
    private GameEventSubscription[] snapshot = Array.Empty<GameEventSubscription>();
    private bool snapshotDirty = true;

    public void addFunction(Action action, int priority = 10, int sequence = 10, bool once = false)
    {
        if (action == null || contains(action)) return;

        insertByOrder(new GameEventSubscription(action, priority, sequence, once));
        snapshotDirty = true;
    }

    public void addFunction(Func<CancellationToken, Awaitable> function, int priority = 10, int sequence = 10, bool once = false)
    {
        if (function == null || contains(function)) return;

        insertByOrder(new GameEventSubscription(function, priority, sequence, once));
        snapshotDirty = true;
    }

    public void removeFunction(Action action)
    {
        if (action == null) return;

        for (int i = 0; i < functions.Count; i++)
        {
            GameEventSubscription subscriber = functions[i];
            if (subscriber.func == null || !Delegate.Equals(subscriber.func, action)) continue;

            functions.RemoveAt(i);
            snapshotDirty = true;
            return;
        }
    }

    public void removeFunction(Func<CancellationToken, Awaitable> function)
    {
        if (function == null) return;

        for (int i = 0; i < functions.Count; i++)
        {
            GameEventSubscription subscriber = functions[i];
            if (subscriber.funcAsync == null || !Delegate.Equals(subscriber.funcAsync, function)) continue;

            functions.RemoveAt(i);
            snapshotDirty = true;
            return;
        }
    }

    public async Awaitable invoke(CancellationToken cancellationToken)
    {
        GameEventSubscription[] currentSnapshot = getSnapshot();
        for (int i = 0; i < currentSnapshot.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameEventSubscription subscriber = currentSnapshot[i];
            if (subscriber.func != null)
            {
                try
                {
                    subscriber.func();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Debug.LogError(exception);
                }
            }
            else if (subscriber.funcAsync != null)
            {
                try
                {
                    await subscriber.funcAsync(cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Debug.LogError(exception);
                }
            }

            if (subscriber.once) removeFunction(subscriber);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private bool contains(Action action)
    {
        for (int i = 0; i < functions.Count; i++)
        {
            Action registeredAction = functions[i].func;
            if (registeredAction != null && Delegate.Equals(registeredAction, action)) return true;
        }

        return false;
    }

    private bool contains(Func<CancellationToken, Awaitable> function)
    {
        for (int i = 0; i < functions.Count; i++)
        {
            Func<CancellationToken, Awaitable> registeredFunction = functions[i].funcAsync;
            if (registeredFunction != null && Delegate.Equals(registeredFunction, function)) return true;
        }

        return false;
    }

    private void removeFunction(GameEventSubscription subscriber)
    {
        for (int i = 0; i < functions.Count; i++)
        {
            if (!ReferenceEquals(functions[i], subscriber)) continue;

            functions.RemoveAt(i);
            snapshotDirty = true;
            return;
        }
    }

    private GameEventSubscription[] getSnapshot()
    {
        if (!snapshotDirty) return snapshot;

        snapshot = functions.ToArray();
        snapshotDirty = false;
        return snapshot;
    }

    private void insertByOrder(GameEventSubscription function)
    {
        int low = 0;
        int high = functions.Count;

        while (low < high)
        {
            int middle = (low + high) >> 1;
            if (functions[middle].CompareTo(function) <= 0) low = middle + 1;
            else high = middle;
        }

        functions.Insert(low, function);
    }
}
