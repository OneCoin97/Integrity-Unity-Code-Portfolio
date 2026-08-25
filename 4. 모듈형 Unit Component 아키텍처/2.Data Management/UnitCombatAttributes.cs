using System;
using UnitComponents;
using UnityEngine;

[Serializable]
public class UnitCombatAttributes
{
    public const UnitState IgnoreEffectMask = UnitState.면역;
    public const UnitState SkillBlockMask = UnitState.스턴 | UnitState.봉인;
    public const UnitState MoveBlockMask = UnitState.스턴 | UnitState.속박;

    [SerializeField] private UnitCombatStatData statData = new UnitCombatStatData();
    [SerializeField] private UnitCombatStatusData statusData = new UnitCombatStatusData();
    [NonSerialized] private SaverForData<UnitCombatStatData> statSaver;
    [NonSerialized] private SaverForData<UnitCombatStatusData> statusSaver;
    [NonSerialized] private bool persistenceEnabled;

    public float attack => statData.attack;
    public float magic => statData.magic;
    public float defense => statData.defense;
    public float range => statData.range;
    public float duration => statData.duration;
    public float armorPenetration => statData.armorPenetration;
    public float hp => statData.hp;
    public float stamina => statData.stamina;
    public float currentStamina => statData.getEffectiveStamina(isUnitStateActive(UnitState.둔화));
    public float MaxHp => statData.MaxHp;
    public float maxStamina => statData.maxStamina;

    public int fov => Mathf.Max(statData.permFov + statData.tempFov + statData.flashFov,1);
    public int currentFov => isUnitStateActive(UnitState.실명) ? 1 : fov;
    public float presenceDetectionRange => Mathf.Max(statData.permPresenceDetection + statData.tempPresenceDetection + statData.flashPresenceDetection,0.01f);
    public bool staminaRemain => statData.staminaRemain;
    public UnitState unitState => getUnitState();
    public UnitState permanentUnitState => statusData.permUnitState;
    public UnitState flashUnitState => statusData.flashUnitState;

    public void initialize(MonoBehaviour owner, string unitName)
    {
        statSaver = new SaverForData<UnitCombatStatData>(statData);
        statSaver.initializeSaver($"{unitName}-CombatStatsData", false);

        statusSaver = new SaverForData<UnitCombatStatusData>(statusData);
        statusSaver.initializeSaver($"{unitName}-CombatStatusesData", false);
        persistenceEnabled = true;
    }

    public void load()
    {
        statSaver.loadImmediate();
        statData.copyFrom(statSaver.data);
        statSaver.data = statData;

        statusSaver.loadImmediate();
        statusData.copyFrom(statusSaver.data);
        statusSaver.data = statusData;
    }

    public void save()
    {
        saveStats();
        saveStatuses();
    }

    public void saveStats()
    {
        if (!persistenceEnabled)
        {
            return;
        }

        statSaver.data = statData;
        statSaver.save();
    }

    public void saveStatuses()
    {
        if (!persistenceEnabled)
        {
            return;
        }

        statusSaver.data = statusData;
        statusSaver.save();
    }

    public void addPermanentStat(UnitStat stat, float value)
    {
        switch (stat)
        {
            case UnitStat.공격력:
                statData.permAttack += value;
                break;
            case UnitStat.주문력:
                statData.permMagic += value;
                break;
            case UnitStat.방어력:
                statData.permDefense += value;
                break;
            case UnitStat.사거리:
                statData.permRange += value;
                break;
            case UnitStat.지속시간:
                statData.permDuration += value;
                break;
            case UnitStat.시야:
                statData.permFov += UnityEngine.Mathf.RoundToInt(value);
                break;
            case UnitStat.인기척:
                statData.permPresenceDetection += UnityEngine.Mathf.RoundToInt(value);
                break;
            case UnitStat.방어력관통:
                statData.permArmorPenetration += value;
                break;
            case UnitStat.체력:
                statData.permHP += value;
                statData.addHP(value);
                break;
            case UnitStat.기력:
                statData.permStamina += value;
                statData.addStamina(value);
                break;
            default:
                return;
        }

        saveStats();
    }

    public float getPermanentStat(UnitStat stat)
    {
        switch (stat)
        {
            case UnitStat.공격력: return statData.permAttack;
            case UnitStat.주문력: return statData.permMagic;
            case UnitStat.방어력: return statData.permDefense;
            case UnitStat.사거리: return statData.permRange;
            case UnitStat.지속시간: return statData.permDuration;
            case UnitStat.체력: return statData.permHP;
            case UnitStat.기력: return statData.permStamina;
            case UnitStat.시야: return statData.permFov;
            case UnitStat.인기척: return statData.permPresenceDetection;
            case UnitStat.방어력관통: return statData.permArmorPenetration;
            default: return 0f;
        }
    }

    public void setPermanentStat(UnitStat stat, float value)
    {
        float difference = value - getPermanentStat(stat);
        addPermanentStat(stat,difference);
    }

    public float getFlashStat(UnitStat stat)
    {
        switch (stat)
        {
            case UnitStat.공격력: return statData.flashAttack;
            case UnitStat.주문력: return statData.flashMagic;
            case UnitStat.방어력: return statData.flashDefense;
            case UnitStat.사거리: return statData.flashRange;
            case UnitStat.지속시간: return statData.flashDuration;
            case UnitStat.체력: return statData.flashHP;
            case UnitStat.기력: return statData.flashStamina;
            case UnitStat.시야: return statData.flashFov;
            case UnitStat.인기척: return statData.flashPresenceDetection;
            case UnitStat.방어력관통: return statData.flashArmorPenetration;
            default: return 0f;
        }
    }

    public void addTemporaryStat(UnitStat stat, float value)
    {
        switch (stat)
        {
            case UnitStat.공격력: statData.tempAttack += value; break;
            case UnitStat.주문력: statData.tempMagic += value; break;
            case UnitStat.방어력: statData.tempDefense += value; break;
            case UnitStat.사거리: statData.tempRange += value; break;
            case UnitStat.지속시간: statData.tempDuration += value; break;
            case UnitStat.체력: statData.tempHP += value; break;
            case UnitStat.기력: statData.tempStamina += value; break;
            case UnitStat.시야: statData.tempFov += Mathf.RoundToInt(value); break;
            case UnitStat.인기척: statData.tempPresenceDetection += Mathf.RoundToInt(value); break;
            case UnitStat.방어력관통: statData.tempArmorPenetration += value; break;
        }
    }

    public void addFlashStat(UnitStat stat, float value)
    {
        switch (stat)
        {
            case UnitStat.공격력: statData.flashAttack += value; break;
            case UnitStat.주문력: statData.flashMagic += value; break;
            case UnitStat.방어력: statData.flashDefense += value; break;
            case UnitStat.사거리: statData.flashRange += value; break;
            case UnitStat.지속시간: statData.flashDuration += value; break;
            case UnitStat.체력: statData.flashHP += value; break;
            case UnitStat.기력: statData.flashStamina += value; break;
            case UnitStat.시야: statData.flashFov += Mathf.RoundToInt(value); break;
            case UnitStat.인기척: statData.flashPresenceDetection += Mathf.RoundToInt(value); break;
            case UnitStat.방어력관통: statData.flashArmorPenetration += value; break;
        }
    }

    public void resetTemporaryStats()
    {
        statData.tempAttack = 0f;
        statData.tempMagic = 0f;
        statData.tempDefense = 0f;
        statData.tempRange = 0f;
        statData.tempDuration = 0f;
        statData.tempHP = 0f;
        statData.tempStamina = 0f;
        statData.tempFov = 0;
        statData.tempPresenceDetection = 0;
        statData.tempArmorPenetration = 0f;
    }

    public UnitCombatStatData createStatSnapshot()
    {
        return statData.deepCopy();
    }

    public void applyStatSnapshot(UnitCombatStatData source)
    {
        statData.copyFrom(source);
    }

    public UnitCombatStatusData createStatusSnapshot()
    {
        return statusData.deepCopy();
    }

    public void applyStatusSnapshot(UnitCombatStatusData source)
    {
        statusData.copyFrom(source);
    }

    public void setPermanentState(UnitState state, bool active)
    {
        if (active)
        {
            statusData.permUnitState |= state;
            statusData.permDisableUnitState &= ~state;
        }
        else
        {
            statusData.permDisableUnitState |= state;
            statusData.permUnitState &= ~state;
        }

        saveStatuses();
    }

    public bool hasPermanentState(UnitState state)
    {
        return state != UnitState.None && (statusData.permUnitState & state) == state;
    }

    public bool hasFlashState(UnitState state)
    {
        return state != UnitState.None && (statusData.flashUnitState & state) == state;
    }

    public void retainTemporaryStates(UnitState mask)
    {
        statusData.tempUnitState &= mask;
    }

    public void setTemporaryState(UnitState state, bool active, bool disabled)
    {
        if (disabled)
        {
            if (active) statusData.disableUnitState |= state;
            else statusData.disableUnitState &= ~state;
            return;
        }

        if (active) statusData.tempUnitState |= state;
        else statusData.tempUnitState &= ~state;
    }

    public void setFlashState(UnitState state, bool active)
    {
        if (active) statusData.flashUnitState |= state;
        else statusData.flashUnitState &= ~state;
    }

    public void resetTemporaryStatuses()
    {
        statusData.tempUnitState = 0;
        statusData.disableUnitState = 0;
    }

    public void clearPermanentStates()
    {
        statusData.permUnitState = 0;
        saveStatuses();
    }

    public float addHP(float value)
    {
        float result = statData.addHP(value);
        saveStats();
        return result;
    }

    public float subHP(float value)
    {
        float result = statData.subHP(value);
        saveStats();
        return result;
    }

    public void setHP(float value)
    {
        statData.setHP(value);
        saveStats();
    }

    public void resetHP()
    {
        statData.resetHP();
        saveStats();
    }

    public void clampHP()
    {
        statData.clampHP();
        saveStats();
    }

    public float addStamina(float value)
    {
        float result = statData.addStamina(value);
        saveStats();
        return result;
    }

    public float subStamina(float value)
    {
        float result = statData.subStamina(value,isUnitStateActive(UnitState.둔화));
        saveStats();
        return result;
    }

    public void setStamina(float value)
    {
        statData.setStamina(value);
        saveStats();
    }

    public void resetStamina()
    {
        statData.resetStamina();
        saveStats();
    }

    public void clampStamina()
    {
        statData.clampStamina();
        saveStats();
    }

    public void invertStamina()
    {
        statData.stamina = statData.maxStamina - statData.stamina;
        saveStats();
    }

    public void clearTemporaryResources()
    {
        statData.tempHP = 0f;
        statData.tempStamina = 0f;
    }

    public bool isFullHP()
    {
        return statData.isFullHP();
    }

    public bool canMoveWithStamina(bool skillAdditionalInput)
    {
        return canMove() && staminaRemain && !skillAdditionalInput;
    }

    public float getStatValue(UnitStat unitStat)
    {
        switch (unitStat)
        {
            case UnitStat.공격력: return attack;
            case UnitStat.주문력: return magic;
            case UnitStat.방어력: return defense;
            case UnitStat.사거리: return range;
            case UnitStat.지속시간: return duration;
            case UnitStat.체력: return MaxHp;
            case UnitStat.기력: return maxStamina;
            case UnitStat.시야: return fov;
            case UnitStat.인기척: return presenceDetectionRange;
            case UnitStat.방어력관통: return armorPenetration;
            default: return 0f;
        }
    }

    public float getMovementStamina(float currentSkillCost)
    {
        return currentStamina + currentSkillCost;
    }

    public void resetCurrentResources()
    {
        statData.resetCurrentResources();
        saveStats();
    }

    public UnitState getUnitState()
    {
        return getUnitState(statusData);
    }

    public bool isUnitStateActive(UnitState state)
    {
        return (getUnitState() & state) == state;
    }

    public bool canSkill()
    {
        UnitState state = getUnitState();
        return (state & IgnoreEffectMask) != 0 || (state & SkillBlockMask) == 0;
    }

    public bool canMove()
    {
        UnitState state = getUnitState();
        return (state & IgnoreEffectMask) != 0 || (state & MoveBlockMask) == 0;
    }

    public static UnitState getUnitState(UnitCombatStatusData statusData)
    {
        return (statusData.permUnitState | statusData.tempUnitState | statusData.flashUnitState) &
               ~statusData.disableUnitState & ~statusData.permDisableUnitState;
    }

    public static bool isUnitStateActive(UnitCombatStatusData statusData, UnitState state)
    {
        return (getUnitState(statusData) & state) == state;
    }

    public static bool canSkill(UnitCombatStatusData statusData)
    {
        UnitState state = getUnitState(statusData);
        return (state & IgnoreEffectMask) != 0 || (state & SkillBlockMask) == 0;
    }

    public static bool canMove(UnitCombatStatusData statusData)
    {
        UnitState state = getUnitState(statusData);
        return (state & IgnoreEffectMask) != 0 || (state & MoveBlockMask) == 0;
    }

    public void dispose()
    {
        if (persistenceEnabled)
        {
            statSaver.removeSaver();
            statusSaver.removeSaver();
        }
    }
}
