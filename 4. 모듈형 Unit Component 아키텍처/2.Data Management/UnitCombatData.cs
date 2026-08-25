using System;
using UnitComponents;
using UnityEngine;

[Serializable]
public class UnitCombatStatData
{
    public bool hasSavedData;
    public float hp;
    public float stamina;

    public float permHP = 15;
    [NonSerialized] public float tempHP;
    [NonSerialized] public float flashHP;
    public float permStamina = 5;
    [NonSerialized] public float tempStamina;
    [NonSerialized] public float flashStamina;
    public int permFov;
    [NonSerialized] public int tempFov;
    [NonSerialized] public int flashFov;
    public int permPresenceDetection;
    [NonSerialized] public int tempPresenceDetection;
    [NonSerialized] public int flashPresenceDetection;

    public float permAttack;
    [NonSerialized] public float tempAttack;
    [NonSerialized] public float flashAttack;
    public float permMagic;
    [NonSerialized] public float tempMagic;
    [NonSerialized] public float flashMagic;
    public float permDefense;
    [NonSerialized] public float tempDefense;
    [NonSerialized] public float flashDefense;
    public float permRange;
    [NonSerialized] public float tempRange;
    [NonSerialized] public float flashRange;
    public float permDuration;
    [NonSerialized] public float tempDuration;
    [NonSerialized] public float flashDuration;
    public float permArmorPenetration;
    [NonSerialized] public float tempArmorPenetration;
    [NonSerialized] public float flashArmorPenetration;

    public float attack => permAttack + tempAttack + flashAttack;
    public float magic => permMagic + tempMagic + flashMagic;
    public float defense => permDefense + tempDefense + flashDefense;
    public float range => permRange + tempRange + flashRange;
    public float duration => permDuration + tempDuration + flashDuration;
    public float armorPenetration => permArmorPenetration + tempArmorPenetration + flashArmorPenetration;
    public float MaxHp => Mathf.Max(permHP + tempHP + flashHP,0.01f);
    public float maxStamina => Mathf.Max(permStamina + tempStamina + flashStamina,0.01f);
    public bool staminaRemain => stamina > 0f;

    public UnitCombatStatData deepCopy()
    {
        return (UnitCombatStatData)MemberwiseClone();
    }

    public void copyFrom(UnitCombatStatData source)
    {
        hasSavedData = source.hasSavedData;
        hp = source.hp;
        stamina = source.stamina;
        permHP = source.permHP;
        tempHP = source.tempHP;
        flashHP = source.flashHP;
        permStamina = source.permStamina;
        tempStamina = source.tempStamina;
        flashStamina = source.flashStamina;
        permFov = source.permFov;
        tempFov = source.tempFov;
        flashFov = source.flashFov;
        permPresenceDetection = source.permPresenceDetection;
        tempPresenceDetection = source.tempPresenceDetection;
        flashPresenceDetection = source.flashPresenceDetection;
        permAttack = source.permAttack;
        tempAttack = source.tempAttack;
        flashAttack = source.flashAttack;
        permMagic = source.permMagic;
        tempMagic = source.tempMagic;
        flashMagic = source.flashMagic;
        permDefense = source.permDefense;
        tempDefense = source.tempDefense;
        flashDefense = source.flashDefense;
        permRange = source.permRange;
        tempRange = source.tempRange;
        flashRange = source.flashRange;
        permDuration = source.permDuration;
        tempDuration = source.tempDuration;
        flashDuration = source.flashDuration;
        permArmorPenetration = source.permArmorPenetration;
        tempArmorPenetration = source.tempArmorPenetration;
        flashArmorPenetration = source.flashArmorPenetration;
    }

    public void resetCurrentResources()
    {
        hp = MaxHp;
        stamina = maxStamina;
    }

    public float getEffectiveStamina(bool isSlowed)
    {
        return isSlowed ? stamina / 2f : stamina;
    }

    public bool isFullHP()
    {
        return hp == MaxHp;
    }

    public void clampHP()
    {
        hp = Mathf.Clamp(hp,0f,MaxHp);
    }

    public void clampStamina()
    {
        stamina = Mathf.Clamp(stamina,0f,maxStamina);
    }

    public float addHP(float value)
    {
        if (value > 0f)
        {
            hp += value;
        }

        clampHP();
        return hp;
    }

    public float subHP(float value)
    {
        if (value > 0f)
        {
            hp -= value;
        }

        clampHP();
        return hp;
    }

    public void setHP(float value)
    {
        hp = Mathf.Clamp(value,0f,MaxHp);
    }

    public void resetHP()
    {
        hp = MaxHp;
    }

    public float addStamina(float value)
    {
        if (value > 0f)
        {
            stamina += value;
        }

        clampStamina();
        return stamina;
    }

    public float subStamina(float value, bool isSlowed)
    {
        if (value > 0f)
        {
            stamina -= isSlowed ? value * 2f : value;
        }

        clampStamina();
        return stamina;
    }

    public void setStamina(float value)
    {
        stamina = Mathf.Clamp(value,0f,maxStamina);
    }

    public void resetStamina()
    {
        stamina = maxStamina;
    }

}

[Serializable]
public class UnitCombatStatusData
{
    public bool hasSavedData;
    public UnitState permUnitState;
    public UnitState permDisableUnitState;
    [NonSerialized] public UnitState tempUnitState;
    [NonSerialized] public UnitState disableUnitState;
    [NonSerialized] public UnitState flashUnitState;

    public UnitCombatStatusData deepCopy()
    {
        return (UnitCombatStatusData)MemberwiseClone();
    }

    public void copyFrom(UnitCombatStatusData source)
    {
        hasSavedData = source.hasSavedData;
        permUnitState = source.permUnitState;
        permDisableUnitState = source.permDisableUnitState;
        tempUnitState = source.tempUnitState;
        disableUnitState = source.disableUnitState;
        flashUnitState = source.flashUnitState;
    }

}
