using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    public enum UnitTargetType
    {
        나자신,
        A모두,
        A모든적,
        A모든아군,
        날타겟했던적들,
        내가타겟했던적들,
        날타겟했던아군들,
        내가타겟했던아군들,
        A이전턴에날타겟했던적들,
        A이전턴에내가타겟했던적들,
        A이전턴에내가타겟했던아군들,
        A이전턴에날타겟했던아군들,
        A이전턴에마지막으로날타겟했던아군,
        A이전턴에마지막으로날타겟했던적군,
        A이전스킬에서타겟한유닛,
        A기습아군,
        A기습적군,
        P이전인덱스에서감지된적군,
        P이전인덱스에서감지된아군,
        P이전에인덱스에서감지된유닛
    }

    public enum SSDTarget
    {
        받은피해량,
        준피해량,
        받은회복량,
        준회복량,
        A이전턴에받은피해량,
        A이전턴에준피해량,
        A이전턴에받은회복량,
        A이전턴에준회복량,
        A이전턴에내가타겟했던적들의수,
        A이전턴에내가타겟했던아군들의수,
        A이전턴에나를타겟했던적들의수,
        A이전턴에나를타겟했던아군들의수,
        A이전턴에내가타겟했던유닛들의수,
        A이전턴에나를타겟했던유닛들의수,
        A이번턴에내가안움직였는지,
        이동거리,
        적을죽인횟수,
        A이전턴에남긴스태미나량,
        A이전턴에행동력을남겼는지,
        A현재체력최대치인지
    }

    [DisallowMultipleComponent]
    public class UnitCombatState : UnitComponent
    {
        private bool isSimulationUnit => unit is SimulationUnit;

        public HashSet<Unit> getTargetUnits(UnitTargetType unitTargetType)
        {
            bool isBrave = unitIdentity.isBrave;

            switch (unitTargetType)
            {
                case UnitTargetType.A모두:
                    List<Unit> allUnits = PartyManager.GetInst.getBraves();
                    allUnits.AddRange(PartyManager.GetInst.getEnemies());
                    return new HashSet<Unit>(allUnits);

                case UnitTargetType.A모든적:
                    return isBrave
                        ? new HashSet<Unit>(PartyManager.GetInst.getEnemies())
                        : new HashSet<Unit>(PartyManager.GetInst.getBraves());

                case UnitTargetType.A모든아군:
                    return isBrave
                        ? new HashSet<Unit>(PartyManager.GetInst.getBraves())
                        : new HashSet<Unit>(PartyManager.GetInst.getEnemies());

                case UnitTargetType.A이전턴에마지막으로날타겟했던적군:
                    return getLastTargeter(combatHistory.getLastEnemyTargeter());

                case UnitTargetType.A이전턴에마지막으로날타겟했던아군:
                    return getLastTargeter(combatHistory.getLastBraveTargeter());

            }

            return new HashSet<Unit>(PartyManager.GetInst.getUnits(combatHistory.getTargetNames(unitTargetType,isBrave)));
        }

        private HashSet<Unit> getLastTargeter(string targeterName)
        {
            if (targeterName == null)
            {
                return new HashSet<Unit>();
            }

            Unit targeter = PartyManager.GetInst.getUnit(targeterName);
            return targeter != null ? new HashSet<Unit> { targeter } : new HashSet<Unit>();
        }

        public void recordTargeting(Unit caster, int skillIndex)
        {
            combatHistory.setSkillInvolved(true);
            bool isSimulation = isSimulationUnit || caster is SimulationUnit;

            if (!isSimulation)
            {
                unitEvent.invoke(UnitEventType.Interaction);
            }

            if (caster == null)
            {
                return;
            }

            combatHistory.setLastInteractionSource(caster);
            if (!isSimulation)
            {
                recordTargetExperience(caster,skillIndex);
            }

            UnitCombatState casterCombatState = caster.getUnitComponent<UnitCombatState>();
            caster.combatHistory.recordTarget(unitIdentity.name,unitIdentity.isBrave,casterCombatState.isUnitTeamTurn());
            combatHistory.recordTargeter(caster.unitIdentity.name,caster.unitIdentity.isBrave,isUnitTeamTurn());

            if (isSimulation)
            {
                return;
            }

            caster.combatHistory.saveTargetHistoryData();
            combatHistory.saveTargetHistoryData();
        }

        private void recordTargetExperience(Unit caster, int skillIndex)
        {
            if (unitIdentity.isBrave == caster.unitIdentity.isBrave)
            {
                unitExp.addUnit(UnitExpUnitType.ROur,caster.unitIdentity.name,-1);
                caster.unitExp.addUnit(UnitExpUnitType.Our,unitIdentity.name,skillIndex);
            }
            else
            {
                unitExp.addUnit(UnitExpUnitType.ROpp,caster.unitIdentity.name,-1);
                caster.unitExp.addUnit(UnitExpUnitType.Opp,unitIdentity.name,skillIndex);
            }
        }

        private bool isUnitTeamTurn()
        {
            if (isSimulationUnit)
            {
                return true;
            }

            CombatTurn currentTurn = CombatTurnManager.GetInst.currentTurn;
            bool braveTurn = currentTurn.CompareTo(CombatTurn.EDelay) < 0;
            return unitIdentity.isBrave == braveTurn;
        }

    }
}
