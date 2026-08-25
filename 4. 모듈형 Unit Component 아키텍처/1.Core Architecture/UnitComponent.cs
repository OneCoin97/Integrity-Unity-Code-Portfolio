using UnityEngine;

namespace UnitComponents
{
    public abstract class UnitComponent : MonoBehaviour
    {
        protected Unit unit { get; private set; }
        protected UnitExp unitExp => unit.unitExp;
        protected Unit.UnitEventManager unitEvent { get; private set; }
        protected Unit.UnitUtility unitUtility { get; private set; }
        protected UnitRuntimeData unitRuntimeData { get; private set; }
        protected UnitIdentity unitIdentity { get; private set; }
        protected UnitTransform unitTransform { get; private set; }
        protected UnitCombatAttributes combatAttributes { get; private set; }
        protected UnitCombatHistory combatHistory { get; private set; }

        protected virtual void Awake()
        {
            unit = GetComponentInParent<Unit>();
            unitEvent = unit.unitEvent;
            unitUtility = unit.unitUtility;
            initializeUnitReferences();
        }

        protected virtual void Start()
        {
        }

        private void initializeUnitReferences()
        {
            unitRuntimeData = unit.unitRuntimeData;
            unitIdentity = unit.unitIdentity;
            unitTransform = unit.unitTransform;
            combatAttributes = unit.combatAttributes;
            combatHistory = unit.combatHistory;
        }

        protected virtual void OnDestroy()
        {
        }
    }
}
