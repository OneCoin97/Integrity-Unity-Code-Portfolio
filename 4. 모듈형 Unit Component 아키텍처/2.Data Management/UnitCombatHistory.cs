using System;
using System.Collections.Generic;
using UnitComponents;
using UnityEngine;

[Serializable]
public class UnitCombatHistory
{
    [SerializeField] private CombatHistoryData data = new CombatHistoryData();
    [SerializeField] private TargetHistoryData targetData = new TargetHistoryData();
    [NonSerialized] private SaverForData<CombatHistoryData> saver;
    [NonSerialized] private SaverForData<TargetHistoryData> targetSaver;

    #region Persistence

    public void initialize(MonoBehaviour owner, string unitName)
    {
        saver = new SaverForData<CombatHistoryData>(data);
        saver.initializeSaver($"{unitName}-CombatHistoryData", false);
        saver.setDelegate(SaverHookType.AfterLoad,afterLoad);

        targetSaver = new SaverForData<TargetHistoryData>(targetData);
        targetSaver.initializeSaver($"{unitName}-TargetHistoryData", false);
        targetSaver.setDelegate(SaverHookType.AfterLoad,afterTargetLoad);
    }

    public void load()
    {
        saver.loadImmediate();
        targetSaver.loadImmediate();
    }

    public void saveCombatHistoryData()
    {
        saver.data = data;
        saver.save();
    }

    public void saveTargetHistoryData()
    {
        targetSaver.data = targetData;
        targetSaver.save();
    }

    public void dispose()
    {
        saver?.removeSaver();
        targetSaver?.removeSaver();
    }

    #endregion

    #region Combat History

    public void applySnapshot(CombatHistoryData source)
    {
        data = source;
    }

    public void applyDefaultData(CombatHistoryData source)
    {
        data = source.deepCopy();
    }

    public CombatHistoryData createSnapshot()
    {
        return data.deepCopy();
    }

    public float getHistoryValue(SSDTarget target)
    {
        if (tryGetTargetValue(target,out float value))
        {
            return value;
        }

        return tryGetCombatValue(target,out value) ? value : 0f;
    }

    private bool tryGetCombatValue(SSDTarget target, out float value)
    {
        switch (target)
        {
            case SSDTarget.받은피해량:
                value = data.receivedDeal;
                return true;
            case SSDTarget.준피해량:
                value = data.dealingAmount;
                return true;
            case SSDTarget.받은회복량:
                value = data.receivedHeal;
                return true;
            case SSDTarget.준회복량:
                value = data.healingAmount;
                return true;
            case SSDTarget.이동거리:
                value = data.confirmedMoveRange + data.tempMoveRange;
                return true;
            case SSDTarget.A이전턴에받은피해량:
                value = data.copiedLastDealingReceived;
                return true;
            case SSDTarget.A이전턴에준피해량:
                value = data.copiedLastDealingAmount;
                return true;
            case SSDTarget.A이전턴에받은회복량:
                value = data.copiedLastHealingReceived;
                return true;
            case SSDTarget.A이전턴에준회복량:
                value = data.copiedLastHealingAmount;
                return true;
            case SSDTarget.A이번턴에내가안움직였는지:
                value = data.isMoved ? 0 : 1;
                return true;
            case SSDTarget.A이전턴에남긴스태미나량:
                value = data.lastStamina;
                return true;
            case SSDTarget.A이전턴에행동력을남겼는지:
                value = data.lastStamina > 0f ? 1f : 0f;
                return true;
            case SSDTarget.적을죽인횟수:
                value = data.killCount;
                return true;
            case SSDTarget.A현재체력최대치인지:
                value = data.isFullHP ? 1 : 0;
                return true;
            default:
                value = 0;
                return false;
        }
    }

    public void reset()
    {
        data.copiedLastDealingAmount = 0f;
        data.copiedLastHealingAmount = 0f;
        data.copiedLastHealingReceived = 0f;
        data.copiedLastDealingReceived = 0f;
        data.lastDealingAmount = 0f;
        data.lastHealingAmount = 0f;
        data.lastHealingReceived = 0f;
        data.lastDealingReceived = 0f;
        data.dealingAmount = 0f;
        data.healingAmount = 0f;
        data.receivedHeal = 0f;
        data.receivedDeal = 0f;
        data.utility = 0f;
        data.isFullHP = false;
        data.isMoved = false;
        data.killCount = 0;
        data.lastStamina = 0f;
        data.confirmedMoveRange = 0f;
        data.tempMoveRange = 0f;
        resetTargetHistory();
    }

    public void beginTurn()
    {
        data.isMoved = false;
        beginTargetTurn();
    }

    public void endTurn(float lastStamina)
    {
        data.copiedLastDealingAmount = data.lastDealingAmount;
        data.copiedLastHealingAmount = data.lastHealingAmount;
        data.copiedLastHealingReceived = data.lastHealingReceived;
        data.copiedLastDealingReceived = data.lastDealingReceived;
        data.lastDealingReceived = 0f;
        data.lastHealingReceived = 0f;
        data.lastDealingAmount = 0f;
        data.lastHealingAmount = 0f;
        data.lastStamina = lastStamina;
        endTargetTurn();
    }

    public void recordDamageReceived(float value, bool unitTeamTurn)
    {
        data.receivedDeal += value;
        if (unitTeamTurn) data.lastDealingReceived += value;
        else data.copiedLastDealingReceived += value;
    }

    public void recordDamageDealt(float value, bool unitTeamTurn)
    {
        data.dealingAmount += value;
        if (unitTeamTurn) data.lastDealingAmount += value;
        else data.copiedLastDealingAmount += value;
    }

    public void recordHealingReceived(float value, bool unitTeamTurn)
    {
        data.receivedHeal += value;
        if (unitTeamTurn) data.lastHealingReceived += value;
        else data.copiedLastHealingReceived += value;
    }

    public void recordHealingDone(float value, bool unitTeamTurn)
    {
        data.healingAmount += value;
        if (unitTeamTurn) data.lastHealingAmount += value;
        else data.copiedLastHealingAmount += value;
    }

    public void recordKill()
    {
        data.killCount++;
    }

    public void markMoved()
    {
        data.isMoved = true;
    }

    public void recordConfirmedMoveRange(float value)
    {
        data.confirmedMoveRange += value;
    }

    public void setTemporaryMoveRange(float value)
    {
        data.tempMoveRange = value;
    }

    public void setFullHealthState(bool value)
    {
        data.isFullHP = value;
    }

    #endregion

    #region Target History

    public void applySnapshot(TargetHistoryData source)
    {
        targetData = source;
    }

    public void applyDefaultData(TargetHistoryData source)
    {
        targetData = source.deepCopy();
    }

    public TargetHistoryData createTargetSnapshot()
    {
        return targetData.deepCopy();
    }

    public Unit getLastInteractionSource()
    {
        return targetData.lastReceiver;
    }

    public void setLastInteractionSource(Unit unit)
    {
        targetData.lastReceiver = unit;
    }

    public bool isSkillInvolved()
    {
        return targetData.isSkillInvolved;
    }

    public void setSkillInvolved(bool value)
    {
        targetData.isSkillInvolved = value;
    }

    public string getLastEnemyTargeter()
    {
        return targetData.copiedLastEnemyTargeter.Count > 0
            ? targetData.copiedLastEnemyTargeter[targetData.copiedLastEnemyTargeter.Count - 1]
            : null;
    }

    public string getLastBraveTargeter()
    {
        return targetData.copiedLastBraveTargeter.Count > 0
            ? targetData.copiedLastBraveTargeter[targetData.copiedLastBraveTargeter.Count - 1]
            : null;
    }

    private bool tryGetTargetValue(SSDTarget target, out float value)
    {
        switch (target)
        {
            case SSDTarget.A이전턴에내가타겟했던적들의수:
                value = targetData.copiedLastEnemyTargets.Count;
                return true;
            case SSDTarget.A이전턴에내가타겟했던아군들의수:
                value = targetData.copiedLastBraveTargets.Count;
                return true;
            case SSDTarget.A이전턴에나를타겟했던적들의수:
                value = targetData.copiedLastEnemyTargeter.Count;
                return true;
            case SSDTarget.A이전턴에나를타겟했던아군들의수:
                value = targetData.copiedLastBraveTargeter.Count;
                return true;
            case SSDTarget.A이전턴에나를타겟했던유닛들의수:
                value = targetData.copiedLastBraveTargeter.Count + targetData.copiedLastEnemyTargeter.Count;
                return true;
            case SSDTarget.A이전턴에내가타겟했던유닛들의수:
                value = targetData.copiedLastEnemyTargets.Count + targetData.copiedLastBraveTargets.Count;
                return true;
            default:
                value = 0f;
                return false;
        }
    }

    public List<string> getTargetNames(UnitTargetType targetType, bool isBrave)
    {
        switch (targetType)
        {
            case UnitTargetType.날타겟했던적들:
                return new List<string>(targetData.enemyTargeterList);
            case UnitTargetType.내가타겟했던적들:
                return new List<string>(targetData.targetedEnemyList);
            case UnitTargetType.날타겟했던아군들:
                return new List<string>(targetData.braveTargeterList);
            case UnitTargetType.내가타겟했던아군들:
                return new List<string>(targetData.targetedBraveList);
            case UnitTargetType.A이전턴에날타겟했던적들:
                return new List<string>(targetData.lastEnemyTargeter);
            case UnitTargetType.A이전턴에내가타겟했던적들:
                return new List<string>(targetData.copiedLastEnemyTargets);
            case UnitTargetType.A이전턴에내가타겟했던아군들:
                return new List<string>(targetData.copiedLastBraveTargets);
            case UnitTargetType.A이전턴에날타겟했던아군들:
                return new List<string>(targetData.copiedLastBraveTargeter);
            case UnitTargetType.A이전스킬에서타겟한유닛:
                List<string> result = new List<string>(targetData.lastSkillBraveTargets);
                result.AddRange(targetData.lastSkillEnemyTargets);
                return result;
            case UnitTargetType.A기습아군:
                return new List<string>(isBrave ? targetData.lastSkillBraveTargets : targetData.lastSkillEnemyTargets);
            case UnitTargetType.A기습적군:
                return new List<string>(isBrave ? targetData.lastSkillEnemyTargets : targetData.lastSkillBraveTargets);
            default:
                return new List<string>();
        }
    }

    public void setLastSkillTargets(IEnumerable<Unit> targets)
    {
        targetData.lastSkillBraveTargets.Clear();
        targetData.lastSkillEnemyTargets.Clear();

        if (targets == null)
        {
            return;
        }

        foreach (Unit target in targets)
        {
            if (target == null)
            {
                continue;
            }

            List<string> targetList = target.unitIdentity.isBrave ? targetData.lastSkillBraveTargets : targetData.lastSkillEnemyTargets;
            addUnique(targetList,target.unitIdentity.name);
        }
    }

    public void recordTarget(string targetName, bool targetIsBrave, bool currentTurn)
    {
        addUnique(targetIsBrave ? targetData.targetedBraveList : targetData.targetedEnemyList,targetName);
        addUnique(currentTurn
            ? targetIsBrave ? targetData.lastBraveTargets : targetData.lastEnemyTargets
            : targetIsBrave ? targetData.copiedLastBraveTargets : targetData.copiedLastEnemyTargets,targetName);
    }

    public void recordTargeter(string casterName, bool casterIsBrave, bool currentTurn)
    {
        addUnique(casterIsBrave ? targetData.braveTargeterList : targetData.enemyTargeterList,casterName);
        addUnique(currentTurn
            ? casterIsBrave ? targetData.lastBraveTargeter : targetData.lastEnemyTargeter
            : casterIsBrave ? targetData.copiedLastBraveTargeter : targetData.copiedLastEnemyTargeter,casterName);
    }

    private void resetTargetHistory()
    {
        targetData.lastReceiver = null;
        targetData.isSkillInvolved = false;
        targetData.lastBraveTargets.Clear();
        targetData.lastEnemyTargets.Clear();
        targetData.targetedBraveList.Clear();
        targetData.targetedEnemyList.Clear();
        targetData.lastSkillBraveTargets.Clear();
        targetData.lastSkillEnemyTargets.Clear();
        targetData.lastBraveTargeter.Clear();
        targetData.lastEnemyTargeter.Clear();
        targetData.braveTargeterList.Clear();
        targetData.enemyTargeterList.Clear();
        targetData.copiedLastBraveTargets.Clear();
        targetData.copiedLastEnemyTargets.Clear();
        targetData.copiedLastBraveTargeter.Clear();
        targetData.copiedLastEnemyTargeter.Clear();
    }

    private void beginTargetTurn()
    {
        targetData.isSkillInvolved = false;
    }

    private void endTargetTurn()
    {
        targetData.copiedLastBraveTargets = new List<string>(targetData.lastBraveTargets);
        targetData.copiedLastEnemyTargets = new List<string>(targetData.lastEnemyTargets);
        targetData.copiedLastBraveTargeter = new List<string>(targetData.lastBraveTargeter);
        targetData.copiedLastEnemyTargeter = new List<string>(targetData.lastEnemyTargeter);
        targetData.lastBraveTargeter.Clear();
        targetData.lastEnemyTargeter.Clear();
        targetData.lastBraveTargets.Clear();
        targetData.lastEnemyTargets.Clear();
        targetData.lastReceiver = null;
    }

    private void addUnique(List<string> list, string value)
    {
        if (!list.Contains(value))
        {
            list.Add(value);
        }
    }

    #endregion

    #region Load Callbacks

    private void afterLoad()
    {
        data = saver.data;
    }

    private void afterTargetLoad()
    {
        targetData = targetSaver.data;
    }

    #endregion
}
