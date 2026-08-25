using System;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class Unit
{
    public sealed class UnitEventManager
    {
        private readonly Dictionary<UnitEventType, Action> unitEvents = new Dictionary<UnitEventType, Action>();

        public void subscribe(UnitEventType type, Action action)
        {
            if (action == null)
            {
                return;
            }

            unitEvents.TryGetValue(type, out Action current);
            current += action;
            unitEvents[type] = current;
        }

        public void unsubscribe(UnitEventType type, Action action)
        {
            if (action == null || !unitEvents.TryGetValue(type, out Action current))
            {
                return;
            }

            current -= action;
            if (current == null)
            {
                unitEvents.Remove(type);
                return;
            }

            unitEvents[type] = current;
        }

        public void invoke(UnitEventType type)
        {
            if (!unitEvents.TryGetValue(type, out Action action))
            {
                return;
            }

            foreach (Action subscriber in action.GetInvocationList())
            {
                try
                {
                    subscriber.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void clear()
        {
            unitEvents.Clear();
        }
    }
}
