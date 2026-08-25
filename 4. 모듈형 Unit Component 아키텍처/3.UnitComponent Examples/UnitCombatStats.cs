using System;
using System.Collections.Generic;
using FloatingTextBuffers;
using UnityEngine;

namespace UnitComponents
{
    [Flags, Serializable]
    public enum UnitStat
    {
        Null = 0,
        공격력 = 1 << 0,
        주문력 = 1 << 1,
        방어력 = 1 << 2,
        사거리 = 1 << 3,
        지속시간 = 1 << 4,
        체력 = 1 << 5,
        기력 = 1 << 6,
        시야 = 1 << 7,
        인기척 = 1 << 8,
        방어력관통 = 1 << 9
    }

    [DisallowMultipleComponent]
    public class UnitCombatStats : UnitComponent
    {
        private readonly CTTManager<SCData> scCTT = new CTTManager<SCData>();
        private bool isSimulationUnit => unit is SimulationUnit;

        public event Action<List<StatData>> statChange;

        protected override void Start()
        {
            base.Start();

            if (isSimulationUnit)
            {
                return;
            }

            scCTT.initialize($"{unitIdentity.name}_UnitStatCTT", removeTimedStat, applyTimedStat);
            scCTT.setOrder(20);
            unitEvent.subscribe(UnitEventType.Load, onUnitLoad);
            unitEvent.subscribe(UnitEventType.Save, onUnitSave);
            unitEvent.subscribe(UnitEventType.Death, resetStats);

            GameManager.GetInst.addFunction(GMEventType.Adventure, resetStats);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            statChange = null;

            if (isSimulationUnit)
            {
                return;
            }

            GameManager.GetInst.removeFunction(GMEventType.Adventure, resetStats);
            unitEvent.unsubscribe(UnitEventType.Load, onUnitLoad);
            unitEvent.unsubscribe(UnitEventType.Save, onUnitSave);
            unitEvent.unsubscribe(UnitEventType.Death, resetStats);
            scCTT.removeSaver();
        }

        public List<StatData> getActiveStatData()
        {
            List<StatData> result = new List<StatData>();
            foreach (TurnTimerData<SCData> timerData in scCTT.dataListClass.dataList)
            {
                (Turn,Turn) restTurn = scCTT.getRestTurnWithWait(timerData.dataNum);
                result.Add(new StatData(timerData.data,restTurn.Item1,restTurn.Item2));
            }

            addFlashStatData(result,UnitStat.시야,combatAttributes.getFlashStat(UnitStat.시야));
            addFlashStatData(result,UnitStat.인기척,combatAttributes.getFlashStat(UnitStat.인기척));
            addFlashStatData(result,UnitStat.공격력,combatAttributes.getFlashStat(UnitStat.공격력));
            addFlashStatData(result,UnitStat.주문력,combatAttributes.getFlashStat(UnitStat.주문력));
            addFlashStatData(result,UnitStat.방어력,combatAttributes.getFlashStat(UnitStat.방어력));
            addFlashStatData(result,UnitStat.사거리,combatAttributes.getFlashStat(UnitStat.사거리));
            addFlashStatData(result,UnitStat.지속시간,combatAttributes.getFlashStat(UnitStat.지속시간));
            addFlashStatData(result,UnitStat.방어력관통,combatAttributes.getFlashStat(UnitStat.방어력관통));
            return result;
        }

        public void addTemporaryStat(UnitStat unitStat, float value, Turn turn, Turn waitTurn = new Turn())
        {
            if (turn.num < 0)
            {
                return;
            }

            if (isSimulationUnit)
            {
                applyTemporaryStat(unitStat,value,false);
                return;
            }

            scCTT.addTurnTimer(new SCData(unitStat,value),turn,waitTurn);
            onStatChanged();
        }

        public void addPermanentStat(UnitStat unitStat, float value)
        {
            combatAttributes.addPermanentStat(unitStat,value);
            onStatChanged();
        }

        public void addFlashStat(UnitStat unitStat, float value)
        {
            combatAttributes.addFlashStat(unitStat,value);
            FloatingTextManager.GetInst.addFloatingText(unit,new FTStat(new SCData(unitStat,value,true)));
            onStatChanged();
        }

        public void clearTimedStats()
        {
            if (isSimulationUnit)
            {
                return;
            }

            scCTT.forcedRemoveTurnTimerWithProcess();
        }

        public void resetStats()
        {
            scCTT.resetData();
            combatAttributes.resetTemporaryStats();
            onStatChanged();
        }

        private void onUnitLoad()
        {
            scCTT.load();
            onStatChanged();
        }

        private void onUnitSave()
        {
            scCTT.save();
        }

        private void onStatChanged()
        {
            List<StatData> statDatas = getActiveStatData();
            if (unit.isSelected && unitIdentity.isBrave)
            {
                CombatUIManager.GetInst.makeUnitStat(statDatas);
            }

            try
            {
                statChange?.Invoke(statDatas);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void applyTimedStat(SCData scData)
        {
            applyTemporaryStat(scData.unitStat,scData.value,false);
            FloatingTextManager.GetInst.addFloatingText(unit,new FTStat(scData));
            onStatChanged();
        }

        private void removeTimedStat(SCData scData)
        {
            applyTemporaryStat(scData.unitStat,scData.value,true);
            scData.value = -scData.value;
            FloatingTextManager.GetInst.addFloatingText(unit,new FTStat(scData));
            onStatChanged();
        }

        private void applyTemporaryStat(UnitStat unitStat, float value, bool undo)
        {
            float appliedValue = undo ? -value : value;
            combatAttributes.addTemporaryStat(unitStat,appliedValue);
            switch (unitStat)
            {
                case UnitStat.체력:
                    if (undo)
                    {
                        combatAttributes.clampHP();
                    }
                    else
                    {
                        combatAttributes.addHP(value);
                    }
                    combatHistory.setFullHealthState(combatAttributes.isFullHP());
                    break;
                case UnitStat.기력:
                    if (undo)
                    {
                        combatAttributes.clampStamina();
                    }
                    else
                    {
                        combatAttributes.addStamina(value);
                    }
                    unit.unitController.recalculateReachableArea();
                    break;
            }
        }

        private void addFlashStatData(List<StatData> result, UnitStat unitStat, float value)
        {
            if (value != 0)
            {
                result.Add(new StatData(new SCData(unitStat,value),new Turn(-1,false),new Turn()));
            }
        }
    }

    [Serializable]
    public struct SCData
    {
        public UnitStat unitStat;
        public float value;
        public bool flash;

        public SCData(UnitStat unitStat, float value, bool flash = false)
        {
            this.unitStat = unitStat;
            this.value = value;
            this.flash = flash;
        }
    }
}
