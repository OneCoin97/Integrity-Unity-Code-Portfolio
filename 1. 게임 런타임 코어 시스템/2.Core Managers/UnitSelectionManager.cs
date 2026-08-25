using System;
using System.Collections.Generic;
using System.Threading;
using UnitComponents;
using UnityEngine;

public sealed class UnitSelectionManager : IPartyDataListener, ICombatTurnDataListener
{
    private static UnitSelectionManager instance;
    public static UnitSelectionManager GetInst => instance ??= new UnitSelectionManager();

    private event Action<Unit, Unit> unitSelectionDataChanged;
    public Unit beforeUnit { get; private set; }
    public Unit selectedUnit { get; private set; }

    private UnitSelectionData selectionData = new UnitSelectionData();
    private readonly SaverForData<UnitSelectionData> saverForData;

    private IReadOnlyList<Brave> braveParty = Array.Empty<Brave>();
    private IReadOnlyList<Enemy> enemyParty = Array.Empty<Enemy>();
    private GameModeType gameModeType;
    private CombatTurn combatTurn;
    private bool unitChangeLock;

    #region Initialization

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void resetInstance()
    {
        instance = null;
    }

    private UnitSelectionManager()
    {
        PartyManager.GetInst.subscribePartyData(this);
        PartyManager.GetInst.unitDieEvent += onUnitDie;
        CombatTurnManager.GetInst.subscribeCombatTurnData(this);

        saverForData = new SaverForData<UnitSelectionData>(selectionData);
        saverForData.initializeSaver("UnitSelection", false);
        saverForData.setOrder(4, 42);
        saverForData.setDelegate(SaverHookType.AfterLoad, afterLoad);

        GameManager.GetInst.addFunction(GMEventType.Adventure, selectInitialAdventureUnit, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, finalizeAdventureSelection, GMEventPhase.After);
        GameManager.GetInst.addFunction(GMEventType.Combat, selectInitialCombatUnit, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Combat, enterCombatMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Adventure, enterAdventureMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.Title, enterTitleMode, GMEventPhase.Before);
        GameManager.GetInst.addFunction(GMEventType.StartNewGame, resetSelectionData);
        GameManager.GetInst.addFunction(GMEventType.ResumeEnding, resumeEnding);
        GameManager.GetInst.addFunction(GMEventType.StartEnding, lockUnitChange, 0, 0);
        GameManager.GetInst.addFunction(GMEventType.StartSkillUpgrade, lockUnitChange);
        GameManager.GetInst.addFunction(GMEventType.EndSkillUpgrade, unlockUnitChange);
        GameManager.GetInst.addFunction(CombatTurn.Delay, startBraveTurn, true);
    }

    #endregion

    #region External API

    public void subscribeUnitSelectionData(IUnitSelectionListener listener)
    {
        if (listener == null) return;
        unitSelectionDataChanged += listener.updateUnitSelectionData;
        listener.updateUnitSelectionData(beforeUnit, selectedUnit);
    }

    public void unsubscribeUnitSelectionData(IUnitSelectionListener listener)
    {
        if (listener != null) unitSelectionDataChanged -= listener.updateUnitSelectionData;
    }

    public bool tryGetSelectedUnit(out Unit unit)
    {
        unit = selectedUnit;
        return unit != null;
    }

    private void setUnitChangeLock(bool value)
    {
        unitChangeLock = value;
    }

    public void setSelectedUnit(Unit unit)
    {
        if (unit == null || unitChangeLock) return;

        beforeUnit = selectedUnit;
        selectedUnit = unit;
        if (beforeUnit != selectedUnit && beforeUnit != null && beforeUnit.gameObject != null) beforeUnit.unitController.unselect();
        selectedUnit.unitController.select();
        unitSelectionDataChanged?.Invoke(beforeUnit, selectedUnit);
        selectionData.selectedUnitName = selectedUnit.unitIdentity.name;
        saverForData.save();
    }

    public void selectUnitByInputNumber(int requestedIndex)
    {
        if (requestedIndex < 0 || braveParty.Count <= requestedIndex) return;
        if (gameModeType == GameModeType.Combat &&
            combatTurn.CompareTo(CombatTurn.EDelay) >= 0) return;

        Brave brave = braveParty[requestedIndex];
        if (brave != null) setSelectedUnit(brave);
    }

    public void selectingNextUnit()
    {
        if (selectedUnit is Brave)
        {
            if (braveParty.Count == 0) return;

            int currentIndex = -1;
            for (int i = 0; i < braveParty.Count; i++)
            {
                if (braveParty[i] == selectedUnit)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                if (braveParty[0] != null) setSelectedUnit(braveParty[0]);
                return;
            }

            int selectCount = braveParty.Count;
            bool selectPrevious = gameModeType == GameModeType.Adventure;
            for (int i = 0; i < selectCount; i++)
            {
                if (selectPrevious)
                {
                    if (--currentIndex < 0) currentIndex = selectCount - 1;
                }
                else if (++currentIndex >= selectCount)
                {
                    currentIndex = 0;
                }

                Brave brave = braveParty[currentIndex];
                if (brave != null)
                {
                    setSelectedUnit(brave);
                    return;
                }
            }
        }
        else
        {
            if (enemyParty.Count == 0) return;

            int nextIndex = 0;
            for (int i = 0; i < enemyParty.Count; i++)
            {
                if (enemyParty[i] != selectedUnit) continue;
                nextIndex = i + 1;
                break;
            }
            if (nextIndex >= enemyParty.Count) nextIndex = 0;
            Unit unit = enemyParty[nextIndex];
            if (unit != null) setSelectedUnit(unit);
        }
    }

    #endregion

    #region Data Subscriptions

    void IPartyDataListener.updatePartyData(IReadOnlyList<Brave> braveParty, IReadOnlyList<Enemy> enemyParty)
    {
        this.braveParty = braveParty;
        this.enemyParty = enemyParty;
    }

    void ICombatTurnDataListener.updateCombatTurnData(int turnCounter, CombatTurn combatTurn, int combatCount)
    {
        this.combatTurn = combatTurn;
    }

    #endregion

    #region Internal Logic

    private void onUnitDie(Unit unit)
    {
        if (!unit.isSelected) return;
        if (unit is Brave && braveParty.Count == 0) return;
        if (unit is Enemy && enemyParty.Count == 0) return;
        selectingNextUnit();
    }

    private async Awaitable resumeEnding(CancellationToken cancellationToken)
    {
        while (selectedUnit == null)
        {
            await Awaitable.NextFrameAsync(cancellationToken);
        }

        await Awaitable.NextFrameAsync(cancellationToken);
        await GameManager.GetInst.invoke(GMEventType.StartEnding, cancellationToken);
    }

    private void startBraveTurn()
    {
        if (braveParty.Count > 0) setSelectedUnit(braveParty[0]);
    }

    private void selectInitialCombatUnit()
    {
        Unit initialUnit = null;
        string selectedUnitName = selectionData.selectedUnitName;
        if (combatTurn.CompareTo(CombatTurn.EDelay) >= 0)
        {
            foreach (Enemy enemy in enemyParty)
            {
                if (enemy != null && enemy.unitIdentity.name.Equals(selectedUnitName)) initialUnit = enemy;
            }
            if (initialUnit == null && enemyParty.Count > 0) initialUnit = enemyParty[0];
        }
        else
        {
            foreach (Brave brave in braveParty)
            {
                if (brave != null && brave.unitIdentity.name.Equals(selectedUnitName)) initialUnit = brave;
            }
            if (initialUnit == null && braveParty.Count > 0) initialUnit = braveParty[0];
        }

        if (initialUnit == null) return;
        unitChangeLock = false;
        setSelectedUnit(initialUnit);
        if (selectedUnit.tryGetUnitComponent(out MoveController moveController)) moveController.stopMove();
    }

    private void selectInitialAdventureUnit()
    {
        unitChangeLock = false;
        if (braveParty.Count > 0) setSelectedUnit(braveParty[0]);
    }

    private void finalizeAdventureSelection()
    {
        if (selectedUnit != null) selectedUnit.unitUtility.collider.isTrigger = false;
    }

    private void unlockUnitChange()
    {
        unitChangeLock = false;
    }

    private void lockUnitChange()
    {
        unitChangeLock = true;
    }

    private void afterLoad()
    {
        selectionData = saverForData.data;
    }

    private void enterCombatMode()
    {
        gameModeType = GameModeType.Combat;
    }

    private void enterAdventureMode()
    {
        gameModeType = GameModeType.Adventure;
    }

    private void enterTitleMode()
    {
        gameModeType = GameModeType.Title;
        unitChangeLock = false;
    }

    private void resetSelectionData()
    {
        selectionData.selectedUnitName = null;
        saverForData.save();
    }

    #endregion
}

[Serializable]
public sealed class UnitSelectionData
{
    public string selectedUnitName;
}
