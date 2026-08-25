using System;
using System.Threading;
using UnitComponents;
using UnityEngine;

public sealed class PartyRecoveryManager : MonoBehaviour
{
    private PartyRecoveryData recoveryData = new PartyRecoveryData();
    private SaverForData<PartyRecoveryData> saverForData;

    private void Awake()
    {
        saverForData = new SaverForData<PartyRecoveryData>(recoveryData);
        saverForData.initializeSaver("PartyRecovery", false);
        saverForData.setOrder(5, 9);
        saverForData.setDelegate(SaverHookType.AfterLoad, afterLoad);

        GameManager.GetInst.addFunction(GMEventType.Retry, healAtRestRoom, 5, 6);
        GameManager.GetInst.addFunction(GMEventType.RestRoom, healAtRestRoom);
        GameManager.GetInst.addFunction(GMEventType.CombatEnd, reserveCombatEndHeal);
        GameManager.GetInst.addFunction(GMEventType.StartNewGame, resetRecoveryData);
        GameManager.GetInst.addFunction(AdventureTurn.Move, healAtEndCombat, true, 0, 1, false);
    }

    private void reserveCombatEndHeal()
    {
        recoveryData.combatEndHeal = true;
        saverForData.save();
    }

    private void afterLoad()
    {
        recoveryData = saverForData.data;
    }

    private async Awaitable healAtEndCombat(CancellationToken cancellationToken)
    {
        if (!recoveryData.combatEndHeal) return;
        while (CameraManager.GetInst.isMoving)
        {
            await Awaitable.NextFrameAsync(cancellationToken);
        }
        await Awaitable.WaitForSecondsAsync(2f, cancellationToken);
        recoveryData.combatEndHeal = false;
        saverForData.save();
        foreach (Brave brave in PartyManager.GetInst.braveParty)
        {
            SoundController.GetInst.playSFX(SFX.etc, brave.transform.position, 2);
            EffectViewModel.getInst.createEffectInHitUnit(brave, 14);
            brave.unitRuntimeData.isDead = false;
            brave.getUnitComponent<UnitCombatStatuses>().cleanse();
            brave.getUnitComponent<UnitCombatResources>().receiveHealing(null,-1,(int)(brave.combatAttributes.MaxHp / 3),false);
            brave.unitController.save();
        }
    }

    private void healAtRestRoom()
    {
        foreach (Brave brave in PartyManager.GetInst.braveParty)
        {
            SoundController.GetInst.playSFX(SFX.etc, brave.transform.position, 2);
            EffectViewModel.getInst.createEffectInHitUnit(brave, 14);
            if (brave.tryGetUnitComponent(out Skill skill)) skill.resetCoolTime();
            brave.getUnitComponent<UnitCombatResources>().receiveHealing(null,-1,(int)brave.combatAttributes.MaxHp,false);
            brave.unitController.save();
        }
    }

    private void resetRecoveryData()
    {
        recoveryData.combatEndHeal = false;
        saverForData.save();
    }
}

[Serializable]
public sealed class PartyRecoveryData
{
    public bool combatEndHeal;
}
