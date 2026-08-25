using System;
using System.Collections.Generic;
using System.Threading;
using UnitComponents;
using UnityEngine;

public sealed class CombatTurnManager
{
    private const float AdventureTurnInterval = 7.5f;

    private static CombatTurnManager instance;
    public static CombatTurnManager GetInst => instance ??= new CombatTurnManager();
    
    private event Action<int, CombatTurn, int> combatTurnDataChanged;

    public CombatTurn currentTurn { get; private set; } = CombatTurn.None;
    public int turnCounter { get; private set; }
    public int combatCount => combatProgressData.combatCount;

    private CombatTurnData combatData = new CombatTurnData();
    private CombatProgressData combatProgressData = new CombatProgressData();

    private SaverForData<CombatTurnData> saverForData;
    private SaverForData<CombatProgressData> combatProgressSaver;
    private Dictionary<CombatTurn, GameMode<CombatTurn>> gameModes;
    private CancellationTokenSource combatModeCancellation;
    private CancellationTokenSource adventureTimerCancellation;
    private bool playingCombatMode;
    private bool restoreCombatAfterLoad;

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance?.stopCombatFlow();
        instance = null;
    }

    private CombatTurnManager()
    {
        instance = this;
        GameManager.GetInst.addFunction(GMEventType.Combat, prepareCombatFlow, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Combat, enterCombatMode, GMEventPhase.After);
        GameManager.GetInst.addFunction(GMEventType.Adventure, prepareAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, enterAdventureFlow, GMEventPhase.After);
        GameManager.GetInst.addFunction(GMEventType.Title, stopCombatFlow, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.StartNewGame, resetCombatData);
        GameManager.GetInst.addFunction(GMEventType.CombatEnd, addCombatCount, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Gameover, completeGameOver, GMEventPhase.Before);

        saverForData = new SaverForData<CombatTurnData>(combatData);
        saverForData.initializeSaver("CombatTurn", false);
        saverForData.setOrder(4, 40);
        saverForData.setDelegate(SaverHookType.AfterLoad, afterLoad);

        combatProgressSaver = new SaverForData<CombatProgressData>(combatProgressData);
        combatProgressSaver.initializeSaver("CombatProgress", true);
        combatProgressSaver.setOrder(0, 1);
        combatProgressSaver.setDelegate(SaverHookType.AfterLoad, afterProgressLoad);
        combatProgressSaver.loadImmediate();

        gameModes = new Dictionary<CombatTurn, GameMode<CombatTurn>>
        {
            { CombatTurn.None, new BraveTurn.None(changeCombatMode) },
            { CombatTurn.Delay, new BraveTurn.Delay(changeCombatMode) },
            { CombatTurn.Ready, new BraveTurn.Ready(changeCombatMode) },
            { CombatTurn.Move, new BraveTurn.Move(changeCombatMode) },
            { CombatTurn.Skill, new BraveTurn.Skill(changeCombatMode) },
            { CombatTurn.Wait, new BraveTurn.Wait(changeCombatMode) },
            { CombatTurn.EDelay, new EnemyTurn.Delay(changeCombatMode) },
            { CombatTurn.EReady, new EnemyTurn.Ready(changeCombatMode) },
            { CombatTurn.EMove, new EnemyTurn.Move(changeCombatMode) },
            { CombatTurn.ESkill, new EnemyTurn.Skill(changeCombatMode) },
            { CombatTurn.EWait, new EnemyTurn.Wait(changeCombatMode) }
        };

        foreach (KeyValuePair<CombatTurn, GameMode<CombatTurn>> mode in gameModes)
        {
            mode.Value.connectEvent(GameManager.GetInst.getCombatModeEvent(mode.Key));
        }
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    #endregion

    #region External API

    public void subscribeCombatTurnData(ICombatTurnDataListener listener)
    {
        if (listener == null) return;
        combatTurnDataChanged += listener.updateCombatTurnData;
        listener.updateCombatTurnData(turnCounter, currentTurn, combatCount);
    }

    public void unsubscribeCombatTurnData(ICombatTurnDataListener listener)
    {
        if (listener != null) combatTurnDataChanged -= listener.updateCombatTurnData;
    }

    private void stopCombatMode()
    {
        if (combatModeCancellation == null) return;
        combatModeCancellation.Cancel();
        combatModeCancellation.Dispose();
        combatModeCancellation = null;
    }

    private void changeCombatMode(CombatTurn combatTurn)
    {
        currentTurn = combatTurn;

        if (currentTurn == CombatTurn.Delay)
        {
            turnCounter++;
        }

        syncCombatData(true);
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private async Awaitable playCombatMode(CancellationToken cancellationToken)
    {
        playingCombatMode = true;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!gameModes.TryGetValue(currentTurn, out GameMode<CombatTurn> activeMode))
                {
                    await Awaitable.NextFrameAsync(cancellationToken);
                    continue;
                }

                await activeMode.update(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            playingCombatMode = false;
        }
    }

    public void requestTurnEnd()
    {
        if (!playingCombatMode) return;
        requestCurrentModeChange(currentTurn.CompareTo(CombatTurn.EDelay) < 0
            ? CombatTurn.Wait
            : CombatTurn.EWait);
    }

    #endregion

    #region Internal Logic

    private void afterLoad()
    {
        combatData = saverForData.data;
        restoreState(combatData.combatTurn, combatData.turnCounter);
        restoreCombatAfterLoad = true;
    }

    private void afterProgressLoad()
    {
        combatProgressData = combatProgressSaver.data;
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void restoreState(CombatTurn combatTurn, int counter)
    {
        int savedTurn = (int)combatTurn;
        if (!Enum.IsDefined(typeof(CombatTurn), combatTurn))
        {
            combatTurn = savedTurn == 10 ? CombatTurn.EReady : CombatTurn.Ready;
        }

        currentTurn = combatTurn;
        turnCounter = counter;
        syncCombatData(false);
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void prepareCombat()
    {
        currentTurn = CombatTurn.Delay;
        syncCombatData(false);
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void prepareLoadedCombat()
    {
        if (currentTurn == CombatTurn.None)
        {
            restoreState(CombatTurn.Ready, turnCounter);
            return;
        }

        if (currentTurn.CompareTo(CombatTurn.EDelay) < 0) return;

        restoreState(CombatTurn.EDelay, turnCounter);
        foreach (Enemy enemy in PartyManager.GetInst.enemyParty)
        {
            enemy.combatAttributes.resetStamina();
            if (enemy.tryGetUnitComponent(out Skill skill)) skill.skillInfoContainer.addTurn();
        }
    }

    private async Awaitable prepareCombatFlow(CancellationToken cancellationToken)
    {
        stopAdventureTimer();
        if (!restoreCombatAfterLoad) await GameManager.GetInst.invoke(GMEventType.StartCombat, cancellationToken);

        if (restoreCombatAfterLoad) prepareLoadedCombat();
        else prepareCombat();
        restoreCombatAfterLoad = false;
        changeCombatMode(currentTurn);
    }

    private void prepareAdventureMode()
    {
        stopCombatMode();
        resetTurnCounter();
    }

    private async Awaitable enterCombatMode(CancellationToken cancellationToken)
    {
        await Awaitable.NextFrameAsync(cancellationToken);
        stopCombatMode();
        combatModeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = playCombatMode(combatModeCancellation.Token);
    }

    private void enterAdventureFlow()
    {
        restoreCombatAfterLoad = false;
        stopAdventureTimer();
        if (combatCount <= 0) return;

        adventureTimerCancellation = new CancellationTokenSource();
        _ = runAdventureTimer(adventureTimerCancellation.Token);
    }

    private void stopAdventureTimer()
    {
        if (adventureTimerCancellation == null) return;
        adventureTimerCancellation.Cancel();
        adventureTimerCancellation.Dispose();
        adventureTimerCancellation = null;
    }

    private void stopCombatFlow()
    {
        stopAdventureTimer();
        stopCombatMode();
    }

    private async Awaitable runAdventureTimer(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Awaitable.WaitForSecondsAsync(AdventureTurnInterval, cancellationToken);
                changeCombatMode(CombatTurn.Delay);
                await GameManager.GetInst.invoke(GMEventType.AT_Brave, cancellationToken);
                changeCombatMode(CombatTurn.EDelay);
                await GameManager.GetInst.invoke(GMEventType.AT_Enemy, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void resetTurnCounter()
    {
        turnCounter = 0;
        syncCombatData(false);
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void addCombatCount()
    {
        combatProgressData.combatCount++;
        combatProgressSaver.save();
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void completeGameOver()
    {
        stopCombatMode();
        addCombatCount();
    }

    private void syncCombatData(bool saveData)
    {
        combatData.combatTurn = currentTurn;
        combatData.turnCounter = turnCounter;
        if (saveData && saverForData != null) saverForData.save();
    }

    private void resetCombatData()
    {
        stopAdventureTimer();
        currentTurn = CombatTurn.None;
        turnCounter = 0;
        restoreCombatAfterLoad = false;
        syncCombatData(true);
        combatTurnDataChanged?.Invoke(turnCounter, currentTurn, combatCount);
    }

    private void requestCurrentModeChange(CombatTurn nextTurn)
    {
        if (gameModes == null) return;
        if (!gameModes.TryGetValue(currentTurn, out GameMode<CombatTurn> activeMode)) return;

        activeMode.changeMode(nextTurn);
    }

    #endregion
}

#region Data Models

[Serializable]
public sealed class CombatTurnData
{
    public CombatTurn combatTurn = CombatTurn.None;
    public int turnCounter;
}

[Serializable]
public sealed class CombatProgressData
{
    public int combatCount;
}

#endregion
