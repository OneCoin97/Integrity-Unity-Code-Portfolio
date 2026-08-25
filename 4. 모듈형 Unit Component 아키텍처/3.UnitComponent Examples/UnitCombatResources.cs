using System;
using System.Collections;
using FloatingTextBuffers;
using UnityEngine;

namespace UnitComponents
{

    public enum DamageType
    {
        Normal,
        BackAttack ,
        Fire,
        Reflection,
    }

    [DisallowMultipleComponent]
    public class UnitCombatResources : UnitComponent
    {
        private UnitCombatStatuses combatStatuses;
        private AnimationManager animationManager;
        private CombatTurn startTurn;
        private CombatTurn endTurn;
        private bool isSimulationUnit => unit is SimulationUnit;

        protected override void Start()
        {
            base.Start();

            if (isSimulationUnit)
            {
                return;
            }

            animationManager = unit.getUnitComponentOrNull<AnimationManager>();

            combatStatuses = unit.getUnitComponent<UnitCombatStatuses>();
            combatStatuses.fireStateTick += fire;

            if (unitIdentity.isBrave)
            {
                startTurn = CombatTurn.Delay;
                endTurn = CombatTurn.EDelay;
            }
            else
            {
                startTurn = CombatTurn.EDelay;
                endTurn = CombatTurn.Delay;
            }

            GameManager.GetInst.addFunction(startTurn,onTurnStart,true);
            GameManager.GetInst.addFunction(endTurn,onTurnEnd,false,0,0);
            GameManager.GetInst.addFunction(GMEventType.Adventure,onAdventure);
            GameManager.GetInst.addFunction(GMEventType.Combat,onCombat);

            unitEvent.subscribe(UnitEventType.Death,onDeath);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (isSimulationUnit)
            {
                return;
            }

            if (combatStatuses != null)
            {
                combatStatuses.fireStateTick -= fire;
            }

            GameManager.GetInst.removeFunction(startTurn,onTurnStart,true);
            GameManager.GetInst.removeFunction(endTurn,onTurnEnd,false);
            GameManager.GetInst.removeFunction(GMEventType.Adventure,onAdventure);
            GameManager.GetInst.removeFunction(GMEventType.Combat,onCombat);

            unitEvent.unsubscribe(UnitEventType.Death,onDeath);
        }

        public float invertHealth(Unit caster, int skillIndex)
        {
            float subValue = combatAttributes.MaxHp - 2f * combatAttributes.hp;

            if (subValue < 0)
            {
                receiveDamage(caster,skillIndex,-subValue);
                return subValue;
            }

            if (subValue > 0)
            {
                receiveHealing(caster,skillIndex,subValue);
                return subValue;
            }

            return 0;
        }

        public void invertStamina()
        {
            float previousStamina = combatAttributes.currentStamina;
            combatAttributes.invertStamina();
            FloatingTextManager.GetInst.addFloatingText(unit,new FTStamina(combatAttributes.currentStamina - previousStamina));
            unit.unitController.recalculateReachableArea();
        }

        public float applyStaminaChange(Unit caster, int skillIndex, float value, bool drain = false)
        {
            float currentStamina = combatAttributes.currentStamina;
            float realValue;

            if (value > 0)
            {
                realValue = combatAttributes.addStamina(value) - currentStamina;
            }
            else
            {
                realValue = combatAttributes.subStamina(-value) - currentStamina;
            }

            if (caster != null)
            {
                caster.unitExp.addAmount(UnitExpAmountType.HealSt,realValue,skillIndex);
            }

            if (caster == unit)
            {
                unitExp.addAmount(UnitExpAmountType.RHealSt,realValue,skillIndex);
            }
            else
            {
                unitExp.addAmount(UnitExpAmountType.RHealSt,realValue,-1);
            }

            FTStamina floatingText = new FTStamina(realValue);
            floatingText.drain = drain;
            FloatingTextManager.GetInst.addFloatingText(unit,floatingText);
            unit.unitController.recalculateReachableArea();
            return realValue;
        }

        public float receiveDamage(Unit caster, int skillIndex, float deal, float piercing = 0, bool extraMode = false, DamageType damageType = DamageType.Normal)
        {
            deal = Mathf.Clamp(deal,0,float.MaxValue);
            prepareDamageType(damageType,caster,ref deal,ref extraMode);
            deal = calculateDamage(deal,piercing,extraMode);

            if (deal <= 0f)
            {
                addDamageFloatingText(unit,0f,damageType);
                return 0f;
            }

            animationManager?.impact();
            applyDamage(caster,deal,damageType);
            recordDamage(caster,skillIndex,deal);
            addDamageFloatingText(unit,deal,damageType);
            return deal;
        }

        private void prepareDamageType(DamageType damageType, Unit caster, ref float deal, ref bool extraMode)
        {
            switch (damageType)
            {
                case DamageType.Normal:
                case DamageType.BackAttack:
                    reflectDamage(caster,deal);
                    break;
                case DamageType.Fire:
                    deal = 0.5f;
                    extraMode = true;
                    break;
                case DamageType.Reflection:
                    break;
            }
        }


        public float calculateExpectedDamage(float deal, float piercing = 0, bool extraMode = false)
        {
            if (combatAttributes.isUnitStateActive(UnitState.무적))
            {
                return 0f;
            }

            return calculateDamageValue(Mathf.Clamp(deal,0f,float.MaxValue),piercing,extraMode,out _,out _);
        }

        private void fire()
        {
            SoundController.GetInst.playSFX(SFX.etc,transform.position,11);
            receiveDamage(null,-1,0.5f,damageType:DamageType.Fire);
        }

        private void reflectDamage(Unit caster, float deal)
        {
            if (caster == null || !combatAttributes.isUnitStateActive(UnitState.반사) || caster == unit)
            {
                return;
            }

            UnitCombatResources casterResources = caster.getUnitComponent<UnitCombatResources>();
            casterResources.receiveDamage(unit,-1,deal,damageType:DamageType.Reflection);
            caster.getUnitComponent<UnitCombatState>().recordTargeting(unit,-1);
        }

        private float calculateDamage(float deal, float piercing, bool extraMode)
        {
            if (combatAttributes.isUnitStateActive(UnitState.무적))
            {
                return 0f;
            }

            deal = calculateDamageValue(deal,piercing,extraMode,out float mitigatedAmount,out float clampedPiercing);
            StartCoroutine(showArmorAndPiercing(mitigatedAmount,clampedPiercing));
            return deal;
        }

        private float calculateDamageValue(float deal, float piercing, bool extraMode, out float mitigatedAmount, out float clampedPiercing)
        {
            if (combatAttributes.isUnitStateActive(UnitState.약점노출) && !extraMode)
            {
                deal += 1f;
            }

            const float minDamage = 0.5f;
            clampedPiercing = Mathf.Clamp(piercing,0f,combatAttributes.defense);
            float effectiveDefense = Mathf.Clamp(combatAttributes.defense - clampedPiercing,0f,float.MaxValue);
            mitigatedAmount = Mathf.Min(effectiveDefense,Mathf.Max(0f,deal - minDamage));

            deal -= mitigatedAmount;
            if (deal <= 0f)
            {
                deal = minDamage;
            }

            return Mathf.Min(combatAttributes.hp,deal);
        }

        private void applyDamage(Unit caster, float deal,DamageType damageType)
        {
            combatHistory.setFullHealthState(false);
            
            if (unit.gameModeType == GameModeType.Adventure)
            {
                if (combatAttributes.subHP(deal) <= 3f)
                {
                    combatAttributes.setHP(3f);
                }

                return;
            }
            
            if (combatAttributes.subHP(deal) <= 0f)
            {
                if (damageType == DamageType.Fire && combatAttributes.isUnitStateActive(UnitState.화상부활))
                {
                    combatAttributes.resetHP();
                    combatAttributes.resetStamina();
                    combatHistory.setFullHealthState(true);
                }
                else
                {
                    unit.unitController.die(caster);
                }
            }
        }

        private void recordDamage(Unit caster, int skillIndex, float deal)
        {
            combatHistory.recordDamageReceived(deal,isUnitTeamTurn());
            if (caster != null)
            {
                caster.unitExp.addAmount(UnitExpAmountType.Deal,deal,skillIndex);
                caster.getUnitComponent<UnitCombatResources>().recordDamageDealt(deal);
            }

            if (caster != unit)
            {
                unitExp.addAmount(UnitExpAmountType.RDeal,deal);
            }
            else
            {
                unitExp.addAmount(UnitExpAmountType.RDeal,deal,skillIndex);
            }

        }

        public float receiveHealing(Unit caster, int skillIndex, float heal, bool useExp = true, bool drain = false)
        {
            heal = Mathf.Clamp(heal,0,float.MaxValue);

            float currentHP = combatAttributes.hp;
            heal = combatAttributes.addHP(heal) - currentHP;

            combatHistory.recordHealingReceived(heal,isUnitTeamTurn());
            if (caster != null)
            {
                caster.getUnitComponent<UnitCombatResources>().recordHealingDone(heal);
            }

            if (useExp)
            {
                if (caster != null)
                {
                    caster.unitExp.addAmount(UnitExpAmountType.Heal,heal,skillIndex);
                }

                if (unit == caster)
                {
                    unitExp.addAmount(UnitExpAmountType.RHeal,heal,skillIndex);
                }
                else
                {
                    unitExp.addAmount(UnitExpAmountType.RHeal,heal);
                }
            }

            FTHeal floatingText = new FTHeal(heal);
            floatingText.drain = drain;
            FloatingTextManager.GetInst.addFloatingText(unit,floatingText);
            updateFullHealthState();
            return heal;
        }

        private void addDamageFloatingText(Unit target, float deal, DamageType damageType)
        {
            FloatingTextManager.GetInst.addFloatingText(target,new FTDeal(deal,damageType));
        }

        private IEnumerator showArmorAndPiercing(float mitigatedAmount, float clampedPiercing)
        {
            yield return null;
            if (mitigatedAmount > 0f)
            {
                FloatingTextManager.GetInst.addFloatingText(unit,new FTArmor(mitigatedAmount));
            }

            if (clampedPiercing > 0f)
            {
                FloatingTextManager.GetInst.addFloatingText(unit,new FTPierce(clampedPiercing));
            }
        }

        private void recordDamageDealt(float deal)
        {
            combatHistory.recordDamageDealt(deal,isUnitTeamTurn());
        }

        private void recordHealingDone(float heal)
        {
            combatHistory.recordHealingDone(heal,isUnitTeamTurn());
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

        private void onTurnStart()
        {
            combatHistory.beginTurn();
            updateFullHealthState();
        }

        private void onTurnEnd()
        {
            combatHistory.endTurn(combatAttributes.currentStamina);
            combatAttributes.resetStamina();
            unit.unitController.recalculateReachableArea();
        }

        private void onAdventure()
        {
            combatHistory.reset();
            combatAttributes.clearTemporaryResources();
            combatAttributes.resetStamina();
            updateFullHealthState();
        }

        private void onCombat()
        {
            updateFullHealthState();
        }

        private void onDeath()
        {
            combatHistory.reset();
        }

        private void updateFullHealthState()
        {
            combatHistory.setFullHealthState(combatAttributes.isFullHP());
        }

    }
}
