using System.Collections;
using UnityEngine;
using UnitComponents;

public abstract partial class Unit
{
    public sealed class UnitController
    {
        private readonly Unit unit;
        private SaverForData<UnitExp> saveForExpData;
        private bool wasMoving;
        private bool saveTransformAfterMove;
        private bool gameManagerEventsRegistered;

        public UnitController(Unit unit)
        {
            this.unit = unit;
            if (unit is SimulationUnit)
            {
                return;
            }

            registerGameManagerEvents();
        }

        private void registerGameManagerEvents()
        {
            GameManager.GetInst.addFunction(GMEventType.StartCombat,captureTransformData);
            gameManagerEventsRegistered = true;
        }

        public void initialize()
        {
            saveForExpData = new SaverForData<UnitExp>(unit.unitExp);
            saveForExpData.initializeSaver($"{unit.unitIdentity.name}-ExpData", true);
            saveForExpData.setDelegate(SaverHookType.AfterLoad,() => unit.unitExp = saveForExpData.data);
            loadExp();
        }

        public void dispose()
        {
            saveForExpData?.removeSaver();
            if (!gameManagerEventsRegistered)
            {
                return;
            }

            GameManager.GetInst.removeFunction(GMEventType.StartCombat,captureTransformData);
            gameManagerEventsRegistered = false;
        }

        public void captureTransformData()
        {
            unit.unitTransform.capture(unit.transform);
        }

        public bool isCastingMove()
        {
            return unit.tryGetUnitComponent(out AnimationManager animationManager) && animationManager.castingMove;
        }

        public void recalculateReachableArea()
        {
            if (unit.gameModeType == GameModeType.Adventure)
            {
                return;
            }

            if (unit is Brave)
            {
                ReachableAreaCoordinator.GetInst.updateNeedReachableAreaRecalculation();
            }
        }

        public void fixedUpdate()
        {
            updateMoveSaveState();

            if (unit.transform.position.y >= -2f || unit.unitRuntimeData.isDead)
            {
                return;
            }

            if (unit.transform.position.y >= -30f)
            {
                return;
            }

            if (unit.gameModeType == GameModeType.Adventure)
            {
                if (unit.isSelected)
                {
                    GameSessionManager.GetInst.fallWhenAdventure();
                }

                return;
            }

            die(null);
            unit.unitExp.addCount(UnitExpCountType.Fall,1);
        }

        public void lateUpdate()
        {
            if (!saveTransformAfterMove || unit.unitRuntimeData.isMove || unit.gameModeType == GameModeType.Title)
            {
                return;
            }

            saveTransformAfterMove = false;
            captureTransformData();
        }

        public void die(Unit killer)
        {
            if (unit is SimulationUnit)
            {
                unit.unitRuntimeData.isDead = true;
                return;
            }

            if (unit.gameModeType == GameModeType.Adventure || unit.unitRuntimeData.isDead)
            {
                return;
            }

            if (killer == null)
            {
                killer = unit.combatHistory.getLastInteractionSource();
            }
            unit.combatAttributes.subHP(float.MaxValue);
            unit.combatAttributes.resetStamina();
            unit.unitRuntimeData.isDead = true;
            unit.unitEvent.invoke(UnitEventType.Death);
            save();

            PartyManager.GetInst.deathUnit(unit);
            recordKill(killer);
            unit.StartCoroutine(deathIE());
        }

        public void unselect()
        {
            unit.unitRuntimeData.skillAdditionalInput = false;
            unit.isSelected = false;
            save();
            unit.unitEvent.invoke(UnitEventType.UnSelect);
        }

        public void select()
        {
            unit.unitRuntimeData.skillAdditionalInput = false;
            unit.isSelected = true;
            unit.unitEvent.invoke(UnitEventType.Select);
        }

        public void save()
        {
            if (unit.gameModeType == GameModeType.Title || unit is SimulationUnit)
            {
                return;
            }

            captureTransformData();
            unit.unitIdentity.save();
            unit.combatAttributes.save();
            unit.combatHistory.saveCombatHistoryData();
            unit.combatHistory.saveTargetHistoryData();
            unit.unitEvent.invoke(UnitEventType.Save);
            saveExp();
        }

        public void load()
        {
            if (unit.gameModeType == GameModeType.Title)
            {
                return;
            }

            unit.unitIdentity.load();
            unit.unitTransform.load();
            unit.combatAttributes.load();
            unit.combatHistory.load();
            unit.unitTransform.apply(unit.transform);
            unit.unitEvent.invoke(UnitEventType.Load);
        }

        private void loadExp()
        {
            saveForExpData?.loadImmediate();
        }

        public void saveExp()
        {
            saveForExpData?.save();
        }

        private void updateMoveSaveState()
        {
            if (unit.unitRuntimeData.isMove)
            {
                wasMoving = true;
                saveTransformAfterMove = false;
                return;
            }

            if (wasMoving)
            {
                wasMoving = false;
                saveTransformAfterMove = true;
            }
        }

        private void recordKill(Unit killer)
        {
            if (killer == null || killer == unit)
            {
                return;
            }

            killer.combatHistory.recordKill();
            killer.unitExp.addCount(UnitExpCountType.Kill,1);
        }

        private IEnumerator deathIE()
        {
            yield return new WaitForSeconds(10);
            MapViewModel.GetInst.deleteFov(unit,true);
        }
    }
}
