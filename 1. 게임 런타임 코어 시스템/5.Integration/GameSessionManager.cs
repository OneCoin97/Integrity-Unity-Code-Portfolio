using System;
using System.Threading;
using ActivingObject;
using UnitComponents;
using UnityEngine;

public sealed class GameSessionManager : MonoBehaviour, IInitialDataReceiver<GameModeFlag>, IUnitSelectionListener
{
    private static GameSessionManager instance;

    public static GameSessionManager GetInst
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameSessionManager>();
            }

            return instance;
        }
    }

    private PartyManager partyManager;
    private UnitSelectionManager unitSelectionManager;

    private bool nextStage;
    private bool falling;
    private bool combatEndTransitioning;
    private bool demoMode;
    private bool devMode;

    private void Awake()
    {
        instance = this;

        partyManager = PartyManager.GetInst;
        unitSelectionManager = UnitSelectionManager.GetInst;
        _ = GameDataManager.GetInst;
        _ = CombatTurnManager.GetInst;
        _ = AdventureTurnManager.GetInst;

        GameManager.GetInst.addFunction(GMEventType.Adventure, resetTransitionState, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Combat, resetTransitionState, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Title, resetTransitionState, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.CombatEnd, prepareCombatEnd, (int)GMEventPhase.Before, 1);
        GameManager.GetInst.addFunction(GMEventType.Gameover, delayGameOver, (int)GMEventPhase.Before, 1);
        partyManager.unitDieEvent += tryStartCombatEnd;
        unitSelectionManager.subscribeUnitSelectionData(this);
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }


    public void ReceiveInitialData(GameModeFlag initialData)
    {
        demoMode = (initialData & GameModeFlag.Demo) != 0;
        devMode = (initialData & GameModeFlag.Dev) != 0;
    }

    public void requestTurnEnd()
    {
        CombatTurnManager combatTurnManager = CombatTurnManager.GetInst;
        if (combatTurnManager.currentTurn.CompareTo(CombatTurn.EDelay) < 0)
        {
            combatTurnManager.requestTurnEnd();
            return;
        }

        if (devMode) combatTurnManager.requestTurnEnd();
    }

    public void triggerOnNextStage()
    {
        nextStage = true;
    }

    public void triggerOnNextStageInAdventure()
    {
        if (GameDataManager.GetInst.currentMode == GameModeType.Adventure)
        {
            triggerOnNextStage();
        }
    }

    public async void fallWhenAdventure()
    {
        if (falling) return;

        falling = true;
        try
        {
            if (nextStage)
            {
                if (demoMode && GameDataManager.GetInst.data.stage == 2)
                {
                    SoundController.GetInst.playSubBGM(0);
                    foreach (Brave brave in partyManager.braveParty)
                    {
                        Destroy(brave.gameObject);
                    }

                    UIButtonFuncManager.GetInst.setNewGameLockLock(true);
                    UIButtonFuncManager.GetInst.setHaveSaveFile(false);
                    UIButtonFuncManager.GetInst.bOnTitle();
                    return;
                }

                GameDataManager.GetInst.advanceStage();
                nextStage = false;
                await GameManager.GetInst.invoke(GMEventType.NextStage, destroyCancellationToken);
                await Awaitable.FixedUpdateAsync(destroyCancellationToken);
                await GameManager.GetInst.invoke(GMEventType.Adventure, destroyCancellationToken);
            }
            else
            {
                await GameManager.GetInst.invoke(GMEventType.Fall, destroyCancellationToken);
                await Awaitable.FixedUpdateAsync(destroyCancellationToken);
                foreach (Brave brave in partyManager.braveParty)
                {
                    if (brave.tryGetUnitComponent(out AnimationManager animationManager))
                    {
                        animationManager.fall = false;
                    }

                    brave.unitUtility.rigidbody.linearVelocity = Vector3.zero;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            falling = false;
        }
    }

    public void updateUnitSelectionData(Unit beforeUnit, Unit selectedUnit)
    {
        ActivingObjectBasis.unitSubscribe(selectedUnit);
    }

    private async void tryStartCombatEnd(Unit _)
    {
        try
        {
            if (combatEndTransitioning) return;
            if (partyManager.enemyParty.Count > 0 && partyManager.braveParty.Count > 0) return;
            if (GameDataManager.GetInst.currentMode != GameModeType.Combat) return;

            combatEndTransitioning = true;

            if (partyManager.enemyParty.Count == 0)
            {
                await gameWin(destroyCancellationToken);
                return;
            }

            await GameManager.GetInst.invoke(GMEventType.Gameover, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Awaitable gameWin(CancellationToken cancellationToken)
    {
        await GameManager.GetInst.invoke(GMEventType.CombatEnd, cancellationToken);
        await Awaitable.NextFrameAsync(cancellationToken);
        await Awaitable.NextFrameAsync(cancellationToken);
        await Awaitable.NextFrameAsync(cancellationToken);

        foreach (Brave brave in partyManager.braveParty)
        {
            brave.unitUtility.setAdventureMode();
        }

        await GameManager.GetInst.invoke(GMEventType.Adventure, cancellationToken);
    }

    private async Awaitable prepareCombatEnd(CancellationToken cancellationToken)
    {
        foreach (Brave brave in partyManager.braveParty)
        {
            brave.unitUtility.setAdventureCollision(true);
        }

        await Awaitable.WaitForSecondsAsync(1, cancellationToken);
        foreach (Brave brave in partyManager.braveParty)
        {
            brave.unitUtility.setAdventureCollision(true);
        }
    }

    private async Awaitable delayGameOver(CancellationToken cancellationToken)
    {
        await Awaitable.WaitForSecondsAsync(2.5f, cancellationToken);
    }

    private void resetTransitionState()
    {
        nextStage = false;
        combatEndTransitioning = false;
    }

    private void OnDestroy()
    {
        GameManager.GetInst.removeFunction(GMEventType.Adventure, resetTransitionState);
        GameManager.GetInst.removeFunction(GMEventType.Combat, resetTransitionState);
        GameManager.GetInst.removeFunction(GMEventType.Title, resetTransitionState);
        GameManager.GetInst.removeFunction(GMEventType.CombatEnd, prepareCombatEnd);
        GameManager.GetInst.removeFunction(GMEventType.Gameover, delayGameOver);
        if (partyManager != null) partyManager.unitDieEvent -= tryStartCombatEnd;

        if (unitSelectionManager != null) unitSelectionManager.unsubscribeUnitSelectionData(this);
        if (instance == this) instance = null;
    }
}
