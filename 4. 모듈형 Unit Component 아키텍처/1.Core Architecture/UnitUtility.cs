using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitComponents;

public abstract partial class Unit
{
    public sealed class UnitUtility
    {
        private readonly Unit unit;
        private GameObject footHold;
        private Coroutine footHoldCo;
        private bool withoutPosition;
        private bool gameManagerEventsRegistered;
        public Rigidbody rigidbody { get; private set; }
        public Collider collider { get; private set; }
        public List<GameObject> childs { get; } = new List<GameObject>();
        public List<GameObject> weapons { get; } = new List<GameObject>();
        public GameObject body { get; private set; }
        public Transform area { get; private set; }
        public Transform light { get; private set; }

        public UnitUtility(Unit unit)
        {
            this.unit = unit;
            initialize();
            unit.unitEvent.subscribe(UnitEventType.Select,applySelectState);
            unit.unitEvent.subscribe(UnitEventType.UnSelect,applyUnselectState);
            unit.unitEvent.subscribe(UnitEventType.Load,applyLoadedPosition);
            unit.unitEvent.subscribe(UnitEventType.Death,applyDeathState);
            if (unit is SimulationUnit)
            {
                return;
            }

            registerGameManagerEvents();
        }

        private void initialize()
        {
            rigidbody = unit.GetComponent<Rigidbody>();
            collider = unit.GetComponent<Collider>();
            body = unit.transform.Find("Body")?.gameObject;
            area = unit.transform.Find("Area");
            light = unit.transform.Find("Light");
            footHold = new GameObject(unit.unitIdentity.name);
            footHold.AddComponent<BoxCollider>();
            footHold.SetActive(false);

            if (body != null)
            {
                collectActiveChildren(body.transform,childs);
            }
        }
        
        public void dispose()
        {
            unit.unitEvent.unsubscribe(UnitEventType.Select,applySelectState);
            unit.unitEvent.unsubscribe(UnitEventType.UnSelect,applyUnselectState);
            unit.unitEvent.unsubscribe(UnitEventType.Load,applyLoadedPosition);
            unit.unitEvent.unsubscribe(UnitEventType.Death,applyDeathState);
            if (gameManagerEventsRegistered)
            {
                GameManager.GetInst.removeFunction(GMEventType.Adventure,setAdventureMode);
                GameManager.GetInst.removeFunction(GMEventType.AdventureUpdate,setAdventureMode);
                GameManager.GetInst.removeFunction(GMEventType.Combat,setCombatMode);
                gameManagerEventsRegistered = false;
            }

            if (footHoldCo != null)
            {
                unit.StopCoroutine(footHoldCo);
            }

            Object.Destroy(footHold);
        }

        private void applySelectState()
        {
            unit.gameObject.layer = LayerMask.NameToLayer("SelectingUnit");
            freezeRotation();
            if (unit.isSelected)
            {
                setAdventureCollision(false);
            }
        }

        private void applyUnselectState()
        {
            unit.gameObject.layer = LayerMask.NameToLayer("Unit");
            freezeRotation();
            if ((unit.gameModeType & GameModeType.Adventure) != 0)
            {
                setAdventureCollision(true);
            }
        }

        public void updateGroundState()
        {
            unit.isGround = Mathf.Abs(rigidbody.linearVelocity.y) < 0.05f;
        }

        private void applyDeathState()
        {
            rigidbody.useGravity = true;
            unit.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            foreach (GameObject child in childs)
            {
                child.layer = LayerMask.NameToLayer("Object");
            }
        }

        public void setAdventureCollision(bool isAdventure)
        {
            freezeRotation();
            if (isAdventure)
            {
                collider.excludeLayers = (1 << LayerMask.NameToLayer("Unit")) |
                                             (1 << LayerMask.NameToLayer("SelectingUnit")) |
                                             (1 << LayerMask.NameToLayer("SelectedUnit")) |
                                             (1 << LayerMask.NameToLayer("Decoration")) |
                                             (1 << LayerMask.NameToLayer("ActiveWall"));
            }
            else
            {
                collider.excludeLayers = (1 << LayerMask.NameToLayer("Unit")) |
                                             (1 << LayerMask.NameToLayer("SelectingUnit")) |
                                             (1 << LayerMask.NameToLayer("SelectedUnit"));
            }
        }

        public void setAdventureMode()
        {
            setGameMode(GameModeType.Adventure);
            setAdventureCollision(!unit.isSelected);
        }

        public void setCombatMode()
        {
            setGameMode(GameModeType.Combat);
            setAdventureCollision(false);
        }

        public void setTitleMode()
        {
            setGameMode(GameModeType.Title);
        }

        private void setGameMode(GameModeType gameModeType)
        {
            unit.gameModeType = gameModeType;
        }

        private void registerGameManagerEvents()
        {
            GameManager.GetInst.addFunction(GMEventType.Adventure,setAdventureMode);
            GameManager.GetInst.addFunction(GMEventType.AdventureUpdate,setAdventureMode);
            GameManager.GetInst.addFunction(GMEventType.Combat,setCombatMode);
            gameManagerEventsRegistered = true;
        }
        
        public void addWeapon(GameObject child)
        {
            childs.Add(child);
            if (unit.tryGetUnitComponent(out BodyEffectController bodyEffectController))
            {
                bodyEffectController.addWeapon(child);
            }

            if (unit.tryGetUnitComponent(out EnemyVisibleState enemyVisibleState))
            {
                enemyVisibleState.addChild(child);
            }

            int layer;
            if (unit.isSelected)
            {
                if (unit.combatAttributes.isUnitStateActive(UnitState.은신) &&
                    !unit.combatAttributes.isUnitStateActive(UnitState.투명))
                {
                    layer = LayerMask.NameToLayer("HideUnit");
                }
                else if (unit.combatAttributes.isUnitStateActive(UnitState.투명))
                {
                    layer = LayerMask.NameToLayer("InvisibleUnit");
                }
                else
                {
                    layer = LayerMask.NameToLayer("Unit");
                }
            }
            else
            {
                if (unit.combatAttributes.isUnitStateActive(UnitState.투명))
                {
                    layer = LayerMask.NameToLayer("InvisibleUnit");
                }
                else if (unit.combatAttributes.isUnitStateActive(UnitState.은신))
                {
                    layer = LayerMask.NameToLayer("HideUnit");
                }
                else
                {
                    layer = LayerMask.NameToLayer("Unit");
                }
            }

            foreach (GameObject weapon in UtilityClass.GetInstance.setLayerIteratively(child, layer))
            {
                weapons.Add(weapon);
            }
        }

        public void collectActiveChildren(Transform parent, List<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    list.Add(child.gameObject);
                }

                if (child.childCount > 0)
                {
                    collectActiveChildren(child, list);
                }
            }
        }

        public Vector3 getAreaPosition()
        {
            if (area != null)
            {
                return area.position;
            }

            return unit.transform.position;
        }

        public void enableFootHold()
        {
            if (footHoldCo != null)
            {
                unit.StopCoroutine(footHoldCo);
            }

            footHoldCo = unit.StartCoroutine(enableFootHoldIE());
        }

        private IEnumerator enableFootHoldIE()
        {
            Vector3 position = unit.transform.position;
            position.y = 0f;
            footHold.transform.position = position;
            footHold.SetActive(true);
            yield return new WaitForSeconds(10f);
            footHold.SetActive(false);
        }
        
        public void setPosition(Vector3 pos)
        {
            withoutPosition = true;
            unit.unitTransform.setPosition(pos);
            unit.transform.position = pos;
            enableFootHold();
        }

        private void applyLoadedPosition()
        {
            if (!withoutPosition)
            {
                Vector3 position = unit.unitTransform.position;
                position.y = 0.5f;
                unit.unitTransform.setPosition(position);
                unit.transform.position = position;
                enableFootHold();
            }

            withoutPosition = false;
        }

        public void setFreeze(bool isFrozen)
        {
            if (rigidbody != null)
            {
                if (isFrozen)
                {
                    rigidbody.constraints = ~RigidbodyConstraints.FreezePositionY & RigidbodyConstraints.FreezeAll;
                }
                else
                {
                    rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                }
            }
        }

        public void freezeRotation()
        {
            if (rigidbody != null)
            {
                rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }

        public void setTemporaryTriggerIgnore(bool ignore)
        {
            LayerMask temporaryIgnoreLayers = LayerMask.GetMask("TriggerArea", "AreaSkill");

            if (rigidbody == null)
            {
                return;
            }

            if (ignore)
            {
                rigidbody.excludeLayers |= temporaryIgnoreLayers;
            }
            else
            {
                rigidbody.excludeLayers &= ~temporaryIgnoreLayers;
            }
        }
    }
}
