using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#region Event Definitions

public enum CombatTurn
{
    None = 0,
    Delay = 1,
    Ready = 2,
    Move = 3,
    Skill = 4,
    Wait = 5,
    EDelay = 7,
    EReady = 8,
    EMove = 9,
    ESkill = 11,
    EWait = 12,
}

public enum AdventureTurn
{
    Move = 0,
    Load = 1,
    Skill = 2
}

[Flags]
public enum GameModeType
{
    None = 0,
    Adventure = 1 << 0,
    Combat = 1 << 1,
    Title = 1 << 2,
}

public enum GMEventType
{
    Adventure = 0,
    AdventureUpdate = 1,
    AT_Brave = 2,
    AT_Enemy = 3,
    StartCombat = 15,
    Combat = 4,
    CombatEnd = 5,
    Title = 6,
    Fall = 7,
    Gameover = 8,
    NextStage = 9,
    StartSkillUpgrade = 10,
    EndSkillUpgrade = 11,
    StartEnding = 12,
    StopEnding = 13,
    Retry = 14,
    ResumeEnding = 18,
    RestRoom = 16,
    StartNewGame =17
}

public enum GMEventPhase
{
    Before = -1000,
    After = 1000
}

#endregion

public sealed class GameManager
{
    private static GameManager instance;

    public static GameManager GetInst => instance ??= new GameManager();

    private readonly Dictionary<GMEventType, GameEventSubscriptions> eventSubscriptions = new();
    private readonly Dictionary<CombatTurn, GameModeEvent> combatModeEvents = createModeEvents<CombatTurn>();
    private readonly Dictionary<AdventureTurn, GameModeEvent> adventureModeEvents = createModeEvents<AdventureTurn>();

    #region Initialization

    private GameManager()
    {
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance = null;
    }

    #endregion

    #region External API

    public IGameModeEvent getCombatModeEvent(CombatTurn turn)
    {
        return combatModeEvents[turn];
    }

    public IGameModeEvent getAdventureModeEvent(AdventureTurn turn)
    {
        return adventureModeEvents[turn];
    }

    public async Awaitable invoke(GMEventType type, CancellationToken cancellationToken = default)
    {
        await getEventSubscriptions(type).invoke(cancellationToken);
    }

    public void addFunction(GMEventType type, Action action, int priority = 10, int sequence = 10, bool once = false)
    {
        getEventSubscriptions(type).addFunction(action, priority, sequence, once);
    }

    public void addFunction(GMEventType type, Action action, GMEventPhase phase, bool once = false)
    {
        addFunction(type, action, (int)phase, 0, once);
    }

    public void addFunction(GMEventType type, Func<CancellationToken, Awaitable> function, int priority = 10, int sequence = 10, bool once = false)
    {
        getEventSubscriptions(type).addFunction(function, priority, sequence, once);
    }

    public void addFunction(GMEventType type, Func<CancellationToken, Awaitable> function, GMEventPhase phase, bool once = false)
    {
        addFunction(type, function, (int)phase, 0, once);
    }

    public void removeFunction(GMEventType type, Action action)
    {
        getEventSubscriptions(type).removeFunction(action);
    }

    public void removeFunction(GMEventType type, Func<CancellationToken, Awaitable> function)
    {
        getEventSubscriptions(type).removeFunction(function);
    }

    public void addFunction(CombatTurn turn, Action action, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        combatModeEvents[turn].addFunction(action, enter, priority, sequence, once);
    }

    public void addFunction(CombatTurn turn, Func<CancellationToken, Awaitable> function, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        combatModeEvents[turn].addFunction(function, enter, priority, sequence, once);
    }

    public void removeFunction(CombatTurn turn, Action action, bool enter)
    {
        combatModeEvents[turn].removeFunction(action, enter);
    }

    public void removeFunction(CombatTurn turn, Func<CancellationToken, Awaitable> function, bool enter)
    {
        combatModeEvents[turn].removeFunction(function, enter);
    }

    public void addFunction(AdventureTurn turn, Action action, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        adventureModeEvents[turn].addFunction(action, enter, priority, sequence, once);
    }

    public void addFunction(AdventureTurn turn, Func<CancellationToken, Awaitable> function, bool enter, int priority = 10, int sequence = 10, bool once = false)
    {
        adventureModeEvents[turn].addFunction(function, enter, priority, sequence, once);
    }

    public void removeFunction(AdventureTurn turn, Action action, bool enter)
    {
        adventureModeEvents[turn].removeFunction(action, enter);
    }

    public void removeFunction(AdventureTurn turn, Func<CancellationToken, Awaitable> function, bool enter)
    {
        adventureModeEvents[turn].removeFunction(function, enter);
    }

    #endregion

    #region Internal Logic

    private static Dictionary<TState, GameModeEvent> createModeEvents<TState>() where TState : Enum
    {
        Dictionary<TState, GameModeEvent> modeEvents = new Dictionary<TState, GameModeEvent>();
        foreach (TState state in Enum.GetValues(typeof(TState)))
        {
            modeEvents.Add(state, new GameModeEvent());
        }

        return modeEvents;
    }

    private GameEventSubscriptions getEventSubscriptions(GMEventType type)
    {
        if (!eventSubscriptions.TryGetValue(type, out GameEventSubscriptions subscriptions))
        {
            subscriptions = new GameEventSubscriptions();
            eventSubscriptions[type] = subscriptions;
        }

        return subscriptions;
    }

    #endregion
}
