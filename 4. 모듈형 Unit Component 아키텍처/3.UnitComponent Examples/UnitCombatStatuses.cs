using System;
using System.Collections.Generic;
using System.Linq;
using FloatingTextBuffers;
using UnityEngine;

namespace UnitComponents
{
    [Flags, Serializable]
    public enum UnitState
    {
        None = 0,
        실드 = 1 << 0,
        무적 = 1 << 1,
        은신 = 1 << 2,
        투명 = 1 << 3,
        반사 = 1 << 4,
        화상부활 = 1 << 5,
        넉백면역 = 1 << 6,
        면역 = 1 << 7,
        둔화 = 1 << 8,
        봉인 = 1 << 9,
        속박 = 1 << 10,
        스턴 = 1 << 11,
        실명 = 1 << 12,
        약점노출 = 1 << 13,
        화상 = 1 << 14,
        발각 = 1 << 15,
        언데드화 = 1 << 16
    }

    [DisallowMultipleComponent]
    public class UnitCombatStatuses : UnitComponent
    {
        private readonly CTTManager<USCData> uscCTT = new CTTManager<USCData>();
        private bool isSimulationUnit => unit is SimulationUnit;

        public event Action stateChange;
        public event Action fireStateTick;

        protected override void Start()
        {
            base.Start();

            if (isSimulationUnit)
            {
                return;
            }

            uscCTT.initialize($"{unitIdentity.name}_UnitStateCTT",removeTimedState,applyTimedState);
            uscCTT.setOrder(20);
            unitEvent.subscribe(UnitEventType.Load, onUnitLoad);
            unitEvent.subscribe(UnitEventType.Save,onUnitSave);
            unitEvent.subscribe(UnitEventType.Death,onDeath);

            GameManager.GetInst.addFunction(CombatTurn.Delay,processTurnStatusEffects,true);
            GameManager.GetInst.addFunction(CombatTurn.EDelay,processTurnStatusEffects,true);
            GameManager.GetInst.addFunction(GMEventType.Adventure,onAdventure);
            GameManager.GetInst.addFunction(GMEventType.Combat,notifyStateChanged);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            stateChange = null;
            fireStateTick = null;

            if (isSimulationUnit)
            {
                return;
            }

            GameManager.GetInst.removeFunction(CombatTurn.Delay,processTurnStatusEffects,true);
            GameManager.GetInst.removeFunction(CombatTurn.EDelay,processTurnStatusEffects,true);
            GameManager.GetInst.removeFunction(GMEventType.Adventure,onAdventure);
            GameManager.GetInst.removeFunction(GMEventType.Combat,notifyStateChanged);
            unitEvent.unsubscribe(UnitEventType.Load, onUnitLoad);
            unitEvent.unsubscribe(UnitEventType.Save,onUnitSave);
            unitEvent.unsubscribe(UnitEventType.Death,onDeath);
            uscCTT.removeSaver();
        }

        public List<StateData> getActiveStateData()
        {
            List<StateData> result = new List<StateData>();
            foreach (TurnTimerData<USCData> timerData in uscCTT.dataListClass.dataList)
            {
                result.Add(new StateData(timerData.data,getDisplayRemainingTurn(timerData)));
            }

            foreach (UnitState state in Enum.GetValues(typeof(UnitState)))
            {
                if (combatAttributes.hasFlashState(state))
                {
                    result.Add(new StateData(new USCData(state,true),new Turn(-1,false)));
                }

                if (combatAttributes.hasPermanentState(state))
                {
                    result.Add(new StateData(new USCData(state,true),new Turn(int.MaxValue,false)));
                }
            }

            return result;
        }

        public void notifyStateChanged()
        {
            if (unit.isSelected)
            {
                CombatUIManager.GetInst.makeUnitState(getActiveStateData());
            }

            stateChange?.Invoke();
        }

        public void processTurnStatusEffects()
        {
            notifyStateChanged();
            if (combatAttributes.isUnitStateActive(UnitState.화상))
            {
                fireStateTick?.Invoke();
            }
        }

        public void cleanse()
        {
            foreach (TurnTimerData<USCData> timerData in uscCTT.dataListClass.dataList.ToList())
            {
                if (UnitState.면역.CompareTo(timerData.data.unitState) < 0)
                {
                    uscCTT.forcedRemoveTurnTimer(timerData);
                }
            }

            int effectIgnoreValue = (int)UnitState.면역;
            int mask = (effectIgnoreValue * 2) - 1;
            combatAttributes.retainTemporaryStates((UnitState)mask);
            FTText floatingText = new FTText();
            floatingText.type = FTTextType.Clean;
            FloatingTextManager.GetInst.addFloatingText(unit,floatingText);
            notifyStateChanged();
        }

        public void removeState(UnitState unitState, bool all)
        {
            foreach (TurnTimerData<USCData> timerData in uscCTT.dataListClass.dataList.ToList())
            {
                if (!unitState.Equals(timerData.data.unitState))
                {
                    continue;
                }

                if (all || timerData.data.isActive)
                {
                    uscCTT.forcedRemoveTurnTimer(timerData);
                }
            }

            combatAttributes.setTemporaryState(unitState,false,false);
            if (all)
            {
                combatAttributes.setTemporaryState(unitState,false,true);
            }

            notifyStateChanged();
        }

        public void clearTimedStatuses()
        {
            if (isSimulationUnit)
            {
                return;
            }

            uscCTT.forcedRemoveTurnTimerWithProcess();
        }

        public void addTimedState(UnitState unitState, bool isActive, Turn turn, Turn waitTurn = new Turn())
        {
            if (turn.num < 0)
            {
                return;
            }

            if (unitState.Equals(UnitState.면역) && isActive)
            {
                cleanse();
            }

            if (isActive && combatAttributes.isUnitStateActive(UnitState.면역) && unitState.CompareTo(UnitState.면역) < 0)
            {
                FloatingTextManager.GetInst.addFloatingText(unit,new FTState(new USCData(UnitState.면역,true)));
                return;
            }

            USCData data = new USCData(unitState,isActive,!isActive);
            if (isSimulationUnit)
            {
                applyTemporaryState(data);
                return;
            }

            uscCTT.addTurnTimer(data,turn,waitTurn);
        }

        public void setPermanentState(UnitState unitState, bool isActive, bool editor = false)
        {
            combatAttributes.setPermanentState(unitState,isActive);

            if (!editor)
            {
                notifyStateChanged();
            }
        }

        public bool hasStateTimer(USCData data)
        {
            foreach (TurnTimerData<USCData> timerData in uscCTT.dataListClass.dataList)
            {
                if (timerData.data.unitState.Equals(data.unitState) && timerData.data.isActive == data.isActive)
                {
                    return true;
                }
            }

            return false;
        }

        public void setFlashState(UnitState unitState, bool isActive)
        {
            combatAttributes.setFlashState(unitState,isActive);
            FloatingTextManager.GetInst.addFloatingText(unit,new FTState(new USCData(unitState,isActive,false,true)));
            notifyStateChanged();
        }

        public void resetStatuses()
        {
            uscCTT.resetData();
            combatAttributes.resetTemporaryStatuses();
        }

        private void onUnitLoad()
        {
            uscCTT.load();
            notifyStateChanged();
        }

        private void onUnitSave()
        {
            uscCTT.save();
        }

        private void onAdventure()
        {
            resetStatuses();
            notifyStateChanged();
        }

        private void onDeath()
        {
            resetStatuses();
        }

        private void applyTimedState(USCData data)
        {
            applyTemporaryState(data);
            FloatingTextManager.GetInst.addFloatingText(unit,new FTState(data));
            notifyStateChanged();
        }

        private void applyTemporaryState(USCData data)
        {
            combatAttributes.setTemporaryState(data.unitState,true,!data.isActive);
        }

        private void removeTimedState(USCData data)
        {
            if (hasStateTimer(data))
            {
                return;
            }

            if (data.unitState == UnitState.화상)
            {
                fireStateTick?.Invoke();
            }

            combatAttributes.setTemporaryState(data.unitState,false,!data.isActive);

            data.isActive = !data.isActive;
            FloatingTextManager.GetInst.addFloatingText(unit,new FTState(data));
            notifyStateChanged();
        }

        private Turn getDisplayRemainingTurn(TurnTimerData<USCData> timerData)
        {
            Turn turn = uscCTT.getRestTurn(timerData.dataNum);
            CustomTurnTimerData turnTimerData = timerData.turnTimerData;
            if (turnTimerData == null || !turnTimerData.waitTurn.isEnd() || turn.num < 0)
            {
                return turn;
            }

            turn.halfTurn = turnTimerData.combatTurn.CompareTo(CombatTurn.EDelay) >= 0;
            return turn;
        }

    }

    [Serializable]
    public struct USCData
    {
        public UnitState unitState;
        public bool isActive;
        public bool flash;
        public bool resist;

        public USCData(UnitState unitState, bool isActive, bool resist = false, bool flash = false)
        {
            this.unitState = unitState;
            this.isActive = isActive;
            this.flash = flash;
            this.resist = resist;
        }

    }
}
