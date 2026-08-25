using System;
using System.Collections.Generic;
using UnityEngine;
using UnitComponents;

public enum UnitClass
{
    Null,
    Knight,
    Thief,
    Sorceress,
    Shaman,
    Priest,
    MiniBoss,
}

public enum UnitEventType
{
    Select,
    UnSelect,
    Death,
    Interaction,
    Save,
    Load,
}

[RequireComponent(typeof(UnitCombatStats), typeof(UnitCombatStatuses), typeof(UnitCombatResources))]
[RequireComponent(typeof(UnitCombatState))]
[DefaultExecutionOrder(-100)]
public abstract partial class Unit : MonoBehaviour
{
    #region Core Systems
    public UnitUtility unitUtility { get; private set; }
    public UnitController unitController { get; private set; }
    public UnitEventManager unitEvent { get; private set; } = new UnitEventManager();

    #endregion

    #region Unit Data
    [field: SerializeField] public UnitIdentity unitIdentity { get; private set; } = new UnitIdentity();
    [field: SerializeField] public UnitTransform unitTransform { get; private set; } = new UnitTransform();
    [field: SerializeField] public UnitCombatAttributes combatAttributes { get; private set; } = new UnitCombatAttributes();
    [field: SerializeField] public UnitCombatHistory combatHistory { get; private set; } = new UnitCombatHistory();
    
    public UnitRuntimeData unitRuntimeData = new UnitRuntimeData();
    public UnitExp unitExp;

    #endregion

    #region Runtime State
    public bool isSelected { get; private set; }
    public bool isGround { get; private set; }
    public GameModeType gameModeType { get; private set; } = GameModeType.None;

    #endregion

    #region UnitComponentDictionary
    private readonly Dictionary<Type, UnitComponent> unitComponents = new Dictionary<Type, UnitComponent>();

    public IReadOnlyDictionary<Type, UnitComponent> UnitComponents => unitComponents;

    private void registerUnitComponent(UnitComponent component)
    {
        if (component == null)
        {
            return;
        }

        Type type = component.GetType();
        while (type != null && type != typeof(UnitComponent) && typeof(UnitComponent).IsAssignableFrom(type))
        {
            unitComponents[type] = component;
            type = type.BaseType;
        }
    }

    protected void rebuildUnitComponentDictionary()
    {
        unitComponents.Clear();
        UnitComponent[] components = GetComponentsInChildren<UnitComponent>(true);
        foreach (UnitComponent component in components)
        {
            registerUnitComponent(component);
        }
    }

    public bool tryGetUnitComponent<T>(out T component) where T : UnitComponent
    {
        if (unitComponents.TryGetValue(typeof(T), out UnitComponent unitComponent))
        {
            component = unitComponent as T;
            return component != null;
        }

        component = null;
        return false;
    }

    public T getUnitComponent<T>() where T : UnitComponent
    {
        if (tryGetUnitComponent(out T component))
        {
            return component;
        }

        Debug.LogError($"{name}에 {typeof(T).Name} 컴포넌트가 없습니다.", this);
        return null;
    }

    public T getUnitComponentOrNull<T>() where T : UnitComponent
    {
        tryGetUnitComponent(out T component);
        return component;
    }

    public bool hasUnitComponent<T>() where T : UnitComponent
    {
        return tryGetUnitComponent<T>(out _);
    }
    
    
    #endregion
    
    #region Unity Lifecycle
    protected virtual void FixedUpdate()
    {
        if (gameModeType == GameModeType.Title)
        {
            return;
        }

        unitUtility.updateGroundState();
        unitController.fixedUpdate();
    }

    protected virtual void LateUpdate()
    {
        unitController.lateUpdate();
    }
    
    protected virtual void Awake()
    {
        initializeUnitCore();

    }

    protected void initializeUnitCore()
    {
        unitController = new UnitController(this);
        unitUtility = new UnitUtility(this);
        rebuildUnitComponentDictionary();
    }
    
    protected virtual void Start()
    {
        unitUtility.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        unitTransform.setVisibleState(VisibleState.Invisible);
        if (gameModeType == GameModeType.Title)
        {
            return;
        }

        unitIdentity.initialize(this);
        unitTransform.initialize(this,unitIdentity.name);
        combatAttributes.initialize(this,unitIdentity.name);
        combatHistory.initialize(this,unitIdentity.name);
        unitController.initialize();
    }
    
    protected virtual void OnDestroy()
    {
        unitController.dispose();
        unitIdentity.dispose();
        unitTransform.dispose();
        combatAttributes.dispose();
        combatHistory.dispose();
        
        unitUtility.dispose();
        unitEvent.clear();
    }
    
    #endregion

}
