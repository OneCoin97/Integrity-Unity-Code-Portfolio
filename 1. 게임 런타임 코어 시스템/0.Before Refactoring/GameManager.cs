using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActivingObject;
using FloatingTextBuffers;
using SkillUpgradeSpace;
using UnitComponents;
using UnityEngine;
using UnityEngine.InputSystem;


public enum CombatTurn
{
    None,
    Delay,
    Ready,
    Move,
    Skill,
    Wait,
    Load,
    EDelay,
    EReady,
    EMove,
    ELoad,
    ESkill,
}

public enum AdventureTurn
{
    Move,
    Load
}

[System.Flags]
public enum GameModeType
{
    None = 0, // 플래그 없음
    Adventure = 1 << 0, // 1
    Combat = 1 << 1, // 2
    Title = 1 << 2, // 4
}

public enum GMEventType
{
    Adventure = 0,
    AdventureUpdate = 1,
    AT_Brave = 2,
    AT_Enemy = 3,
    StartCombat = 15,
    Combat = 4,
    CombatEnd = 5,
    Title = 6,
    Fall = 7,
    Gameover = 8,
    NextStage = 9,
    StartSkillUpgrade = 10,
    EndSkillUpgrade = 11,
    StartEnding = 12,
    StopEnding = 13,
    Retry = 14,
}

public partial class GameManager : MonoBehaviour, UnitsDataListner ,IInitialDataReceiver<GameModeFlag>
{
    public static GameManager GetInst
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                    Debug.Log("해당스크립트를 하이어락키에 추가 하십시오");
            }

            return instance;
        }
    }
    private static GameManager instance = null;
    
    public delegate void OnCombatTurnChanged(int turn, CombatTurn combatTurn);
    public delegate void getUnitData(Unit selectedUnit, List<Brave> Braveparty, List<Enemy> Enemyparty, int unitNum);
    public delegate void getCGameMode(GameModeType type);

    public event OnCombatTurnChanged turnCountManager;
    public event getUnitData unitDataManager;
    public event getCGameMode gamemodeManager;
    public event Action<GameManagerExpData> expDataManager;
    public event Action<Unit> unitDieEvent; 
    
    private Dictionary<CombatTurn, GameMode> CombatModeFunctions; //딕셔너리를 이용한 상태패턴
    private Dictionary<AdventureTurn, GameMode> AdventureModeFunctions;

    private Coroutine CoGameMode;
    [Header("GameState")] 
    [SerializeField] private GameManagerData gameManagerData = new GameManagerData();
    [SerializeField] private GameManagerExpData expData = new GameManagerExpData();

    public Unit selectedUnit;// { get; private set;} //현재 선택된 용사
    [SerializeField] private List<Brave> Braveparty; //파티 구성
    [SerializeField] private List<Enemy> Enemyparty;
    [SerializeField] private List<Unit> currentParty;

    [Space(20)]
    [Header("Setting")]
    [SerializeField] private Transform braveSpace;
    [SerializeField] private Transform enemySpace;


    private bool devMode;
    private bool demoMode;
    [HideInInspector]public bool charChange = false;
    private int partySize; //현재 파티 인원
    
    public Canvas canvas;
    public bool forceNextTurn;
    private bool falling = false;

    private MainInputAction inputAction;
    private bool unitChangePress;
    private bool unitNumPress;
    private int unitNum;
    
    public bool combatChanging;
   
    private Coroutine coroutine;
    [SerializeField] private float adventureTurnDist = 2.5f;
    
    private Coroutine updateBraveCo;

    private SaverForData<GameManagerData> saverForGMData;
    private SaverForData<GameManagerExpData> expSaverForData;

    public bool customMode;
    private UnitsData unitsData = new UnitsData();

    public bool unitChangeLock;
    
    
    public GameModeType getGameModeType
    {
        get { return gameManagerData.cModeType; }
    }
    
     // 각 이벤트 타입별 구독 목록
    private readonly Dictionary<GMEventType, List<GMSubscribeFunc>> table =
        new Dictionary<GMEventType, List<GMSubscribeFunc>>();

    // 제거를 위해 원본 델리게이트(Action/Func) → GMSubscribeFunc 매핑 보관
    private readonly Dictionary<GMEventType, Dictionary<Action, GMSubscribeFunc>> indexAction =
        new Dictionary<GMEventType, Dictionary<Action, GMSubscribeFunc>>();

    private readonly Dictionary<GMEventType, Dictionary<Func<IEnumerator>, GMSubscribeFunc>> indexIE =
        new Dictionary<GMEventType, Dictionary<Func<IEnumerator>, GMSubscribeFunc>>();
    
    [SerializeField]
    private bool nextStage = false;
    
    private List<GMFunctionWaitData> waitList = new();
    private bool run; 
    
    // ====== Public API: Invoke (순차 실행) ======
    public void invoke(GMEventType type)
    {
        StartCoroutine(invokeIE(type));
    }
    
    private IEnumerator safeYield(IEnumerator e)
    {
        while (true)
        {
            bool moveNext;

            try
            {
                moveNext = e.MoveNext();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                yield break;
            }

            if (!moveNext)
                yield break;

            yield return e.Current;
        }
    }
    public IEnumerator invokeIE(GMEventType type)
    {
        run = true;
        List<GMSubscribeFunc> list = getList(type);
        if (list.Count == 0)
        {
            run = false;
            yield break;
        }

        List<int> removeIndicesBuffer = new();

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            GMSubscribeFunc f = list[i];
            if (f == null)
            {
                continue;
            }

            if (f.func != null)
            {
                try
                {
                    f.func();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }

            if (f.funcIE != null)
            {
                if (f.funcIE != null)
                {
                    IEnumerator e = f.funcIE();
                    if (e != null)
                    {
                        yield return safeYield(e); // 여기만 바꿔라
                    }
                }

            }

            if (f.once)
            {
                removeIndicesBuffer.Add(i);
            }
        }

        // once 항목 역순 제거
        if (removeIndicesBuffer.Count > 0)
        {
            for (int k = removeIndicesBuffer.Count - 1; k >= 0; k--)
            {
                int idx = removeIndicesBuffer[k];
                GMSubscribeFunc f = list[idx];

                // 액션/IE 인덱스에서도 제거
                if (f.func != null)
                {
                    Dictionary<Action, GMSubscribeFunc> amap = getActionMap(type);
                    // 동일 키 제거
                    removeFromActionIndex(amap, f);
                }
                if (f.funcIE != null)
                {
                    Dictionary<Func<IEnumerator>, GMSubscribeFunc> imap = getIEMap(type);
                    removeFromIEIndex(imap, f);
                }

                list.RemoveAt(idx);
            }
            removeIndicesBuffer.Clear();
        }

        run = false;
        yield return null;
        processWaitList();
    }

    // ====== 선택: 즉시 실행(코루틴 미사용) ======
    private void invokeImmediate(GMEventType type)
    {
        // 코루틴이 아닌 Action만 즉시 실행
        List<GMSubscribeFunc> list = getList(type);
        if (list.Count == 0)
        {
            return;
        }

        List<int> removeIndicesBuffer = new();

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            try
            {
                GMSubscribeFunc f = list[i];
                if (f == null)
                {
                    continue;
                }

                if (f.func != null)
                {
                    f.func();
                }

                if (f.once)
                {
                    removeIndicesBuffer.Add(i);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        if (removeIndicesBuffer.Count > 0)
        {
            for (int k = removeIndicesBuffer.Count - 1; k >= 0; k--)
            {
                int idx = removeIndicesBuffer[k];
                GMSubscribeFunc f = list[idx];

                if (f.func != null)
                {
                    Dictionary<Action, GMSubscribeFunc> amap = getActionMap(type);
                    removeFromActionIndex(amap, f);
                }
                if (f.funcIE != null)
                {
                    Dictionary<Func<IEnumerator>, GMSubscribeFunc> imap = getIEMap(type);
                    removeFromIEIndex(imap, f);
                }

                list.RemoveAt(idx);
            }
            removeIndicesBuffer.Clear();
        }
    }
    
    private void addToWaitList(GMEventType type, GMSubscribeFunc func, bool isAdd, bool isAction)
    {
        // 1. 이미 동일 func가 waiting에 있는지 확인
        for (int i = 0; i < waitList.Count; i++)
        {
            var w = waitList[i];
            if (w.IsSame(type, func, isAction))
            {
                // 같은 func에 대해 add/remove가 왔으면 상태 업데이트
                w.isAdd = isAdd;
                return;
            }
        }

        // 2. 없으면 신규로 넣기
        waitList.Add(new GMFunctionWaitData(type, func, isAdd, isAction));
    }
    
    private void processWaitList()
    {
        if (waitList.Count == 0) return;

        foreach (var w in waitList)
        {
            if (w.isAction)
            {
                var map = getActionMap(w.type);
                var list = getList(w.type);

                if (w.isAdd)
                {
                    binaryInsert(list, w.func);
                    map[(Action)w.func.func] = w.func;
                }
                else
                {
                    map.Remove((Action)w.func.func);
                    list.Remove(w.func);
                }
            }
            else
            {
                var map = getIEMap(w.type);
                var list = getList(w.type);

                if (w.isAdd)
                {
                    binaryInsert(list, w.func);
                    map[(Func<IEnumerator>)w.func.funcIE] = w.func;
                }
                else
                {
                    map.Remove((Func<IEnumerator>)w.func.funcIE);
                    list.Remove(w.func);
                }
            }
        }

        waitList.Clear();
    }

    public void addCombatCount()
    {
        expData.combatCount++;
        expSaverForData.save();
        invokeExpData();
    }

    public int getCombatCount()
    {
        return expData.combatCount;
    }

    public GameManagerExpData getExpData()
    {
        return expData;
    }

    public void startNewgame()
    {
        expData.stage = 1;
        expSaverForData.save();
        invokeExpData();
    }

    private void invokeExpData()
    {
        expDataManager?.Invoke(expData);
    }

    public void subscribe(UnitsData unitsData)
    {
        this.unitsData = unitsData;
    }
    private void Awake()
    {
        var gameManager = GameManager.GetInst;
        if (GetComponent<EnemyUnitChangeUICycler>() == null)
        {
            gameObject.AddComponent<EnemyUnitChangeUICycler>();
        }

        CombatModeFunctions = new Dictionary<CombatTurn, GameMode>()
        {
            { CombatTurn.None ,new BraveTurn.None(this)},//Dummy 데이터
            /// 순서대로 나열함
            { CombatTurn.Delay, new BraveTurn.Delay(this)},//턴시작시 한번만 실행
                { CombatTurn.Ready ,new BraveTurn.Ready(this)}, //턴시작시 한번만 실행
                    { CombatTurn.Move, new BraveTurn.Move(this)},//여러번 실행됨
                    { CombatTurn.Skill, new BraveTurn.Skill(this)},//여러번 실행됨
                    { CombatTurn.Wait, new BraveTurn.Wait(this)},//여러번 실행됨
                    { CombatTurn.Load, new BraveTurn.Load(this)}, //여러번 실행됨
                
            { CombatTurn.EDelay, new EnemyTurn.Delay(this)},//턴시작시 한번만 실행
                { CombatTurn.EReady ,new EnemyTurn.Ready(this)}, //턴시작시 한번만 실행
                    { CombatTurn.EMove, new EnemyTurn.Move(this)},//여러번 실행됨
                    { CombatTurn.ESkill, new EnemyTurn.Skill(this)},//여러번 실행됨
                    { CombatTurn.ELoad, new EnemyTurn.Load(this)}, //여러번 실행됨
        };

        AdventureModeFunctions = new Dictionary<AdventureTurn, GameMode>()
        {
            { AdventureTurn.Move, new AdventureMode.Move(this) },
            { AdventureTurn.Load, new AdventureMode.Load(this) }
        };

        foreach (var VARIABLE in CombatModeFunctions.Values)
        {
            VARIABLE.start();
        }
        foreach (var VARIABLE in AdventureModeFunctions.Values)
        {
            VARIABLE.start();
        }

        inputAction = new MainInputAction();
        inputAction.Enable();
        inputAction.Map.TurnEnd.performed += onTurnEndPressed;
        inputAction.Map.SelectUnit1.performed += onSeleteUnit1Pressed;
        inputAction.Map.SelectUnit2.performed += onSeleteUnit2Pressed;
        inputAction.Map.SelectUnit3.performed += onSeleteUnit3Pressed;
        inputAction.Map.SelectUnit4.performed += onSeleteUnit4Pressed;


        addFunction(GMEventType.Retry, doAdventureModeIE,5,5);
        addFunction(GMEventType.Retry, healAtRestRoom,5,6);
        
        addFunction(GMEventType.Adventure, () =>
        {
            unitChangeLock = false;
        });
        
        addFunction(GMEventType.Combat, () =>
        {
            unitChangeLock = false;
        });

    
        addFunction(GMEventType.StartEnding, () =>
        {
            unitChangeLock = true;
        },0,0);
        
        addFunction(GMEventType.StartSkillUpgrade, () =>
        {
            unitChangeLock = true;
            selectedUnit.unitRuntimeData.moveLock = true;
            if (selectedUnit.tryGetUnitComponent(out AnimationManager animationManager))
            {
                animationManager.pain = true;
            }

        },0);
        addFunction(GMEventType.EndSkillUpgrade, () =>
        {
            if (selectedUnit.tryGetUnitComponent(out AnimationManager animationManager))
            {
                animationManager.pain = false;
            }

            selectedUnit.unitRuntimeData.moveLock = false;
            unitChangeLock = false;
        },0);

        addFunction(GMEventType.StartEnding, () => { gameManagerData.endStage = true; },0,0);
        addFunction(AdventureTurn.Move, () => { StartCoroutine(healAtEndCombat()); }, true,0,1);
        UnitViewModel.GetInst.subscribe(this);
    }
    private void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60; 
        gameManagerData.adventureTurn = AdventureTurn.Move;
        subscriber();

        saverForGMData = new SaverForData<GameManagerData>(gameManagerData);
        saverForGMData.initialize(this,"GameManager");
        saverForGMData.setOrder(5);
        saverForGMData.setDelegate(SaverHookType.AfterLoad,afterLoad);

        expSaverForData = new SaverForData<GameManagerExpData>(expData);
        expSaverForData.initialize(this,"GameMangerExp",true);
        expSaverForData.setOrder(0, 0);
        expSaverForData.setDelegate(SaverHookType.AfterLoad, expAfterLoad);
        expSaverForData.load();
    }
    private void OnDestroy()
    {
        inputAction.Map.TurnEnd.performed -= onTurnEndPressed;
        inputAction.Map.SelectUnit1.performed -= onSeleteUnit1Pressed;
        inputAction.Map.SelectUnit2.performed -= onSeleteUnit2Pressed;
        inputAction.Map.SelectUnit3.performed -= onSeleteUnit3Pressed;
        inputAction.Map.SelectUnit4.performed -= onSeleteUnit4Pressed;
        
        inputAction.Disable(); // 추가로 비활성화까지
        inputAction.Dispose();
        clearAllEvents();
    }
    
    
    #region InputAction
    
    private void onTurnEndPressed(InputAction.CallbackContext context)
    {
        if(InputManager.isModifierAction)
            return;

        turnEnd();
    }
    private void onSeleteUnit1Pressed(InputAction.CallbackContext context)
    {
        if(InputManager.isModifierAction)
            return;
        
        unitNum = 0;
        unitNumPress = true;
    }
    private void onSeleteUnit2Pressed(InputAction.CallbackContext context)
    {
        if(InputManager.isModifierAction)
            return;
        
        unitNum = 1;
        unitNumPress = true;
    }
    private void onSeleteUnit3Pressed(InputAction.CallbackContext context)
    {
        if(InputManager.isModifierAction)
            return;
        
        unitNum = 2;
        unitNumPress = true;
    }
    private void onSeleteUnit4Pressed(InputAction.CallbackContext context)
    {
        if(InputManager.isModifierAction)
            return;

        unitNum = 3;
        unitNumPress = true;
    }
    public void requestUnitChange()
    {
        unitChangePress = true;
    }

    public void selectBraveFromEnemyUIChange(Brave brave)
    {
        gameManagerData.unitNumber = currentParty.IndexOf(brave);
        setSelectedUnit(brave);
    }
    #endregion


    public void retryCombat()
    {
        foreach (var VARIABLE in Enemyparty)
        {
            Destroy(VARIABLE.gameObject);
        }

        Enemyparty.Clear();
        unitDataInvoke();

        invoke(GMEventType.Retry);
    }
    
    public void triggerOnNextStage()
    {
        nextStage = true;
    }

    public void triggerOnNextStageInAdventure()
    {
        if (!gameManagerData.cModeType.Equals(GameModeType.Adventure))
        {
            return;
        }

        triggerOnNextStage();
    }

    public void stopAllMode()
    {
        if (CoGameMode != null)
        {
            StopCoroutine(CoGameMode);
        }
    }
    
    public IEnumerator destoryAllUnit()
    {
        foreach (var VARIABLE in Braveparty)
        {
            if (VARIABLE != null && VARIABLE.gameObject != null)
            {
                Destroy(VARIABLE.gameObject);
                yield return null;
            }
        }

        foreach (var VARIABLE in Enemyparty)
        {
            if (VARIABLE != null && VARIABLE.gameObject != null)
            {
                Destroy(VARIABLE.gameObject);
                yield return null;
            }
        }

        Braveparty.Clear();
        Enemyparty.Clear();
        currentParty.Clear();
        unitDataInvoke();
    }
    private void save()
    {
        saverForGMData.save();
    }
  
    private void setGameMode(GameModeType type)
    {
        gameManagerData.cModeType = type;

        try
        {
            gamemodeManager?.Invoke(type);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public CombatTurn getCurrentCombatTurn()
    {
        return gameManagerData.combatTurn;
    }
    public bool isCombatMode()
    {
        return gameManagerData.cModeType.Equals(GameModeType.Combat);
    }

    private void expAfterLoad()
    {
        expData = expSaverForData.data;
        invokeExpData();
    }
    
    private void afterLoad()
    {
        gameManagerData = saverForGMData.data;
        //gameManagerData.adventureTurn = AdventureTurn.Load;
        if (gameManagerData.cModeType.Equals(GameModeType.Combat))
        {
            if (gameManagerData.combatTurn.CompareTo(CombatTurn.EDelay) >= 0)
            {
                gameManagerData.combatTurn = CombatTurn.EDelay;
                currentParty = new List<Unit>(Enemyparty);
                foreach (var VARIABLE in Enemyparty)
                {
                    VARIABLE.combatAttributes.resetStamina();
                    if (VARIABLE.tryGetUnitComponent(out UnitComponents.Skill skill))
                    {
                        skill.skillInfoContainer.addTurn();
                    }
                }
            }
            else
            {
                currentParty = new List<Unit>(Braveparty);
            }

            StartCoroutine(doCombatModeIE());
        }
        else
        {
            doAdventureMode();
            if (gameManagerData.endStage)
            {
                StartCoroutine(waitForEnding());
            }
        }
    }

    private IEnumerator waitForEnding()
    {
        yield return new WaitUntil(() => selectedUnit != null);
        yield return null;
        invoke(GMEventType.StartEnding);
    }


    public void updateParty(List<string> names,bool skipEvent = false)
    {
        if (updateBraveCo != null)
        {
            StopCoroutine(updateBraveCo);
        }
        
        updateBraveCo = StartCoroutine(updateBravePartyIE(names,skipEvent));
    }
    public IEnumerator updateBravePartyIE(List<string> names,bool skipEvent)
    {
        // 방어 코드
        if (names == null || names.Count == 0) yield break;
        if (Braveparty == null || Braveparty.Count == 0) yield break;

        // 기존 파티 보존
        List<Brave> temp = Braveparty.ToList();
        Braveparty = new List<Brave>();

        // ① names 순서대로 삽입
        for (int i = 0; i < names.Count; i++)
        {
            string targetName = names[i];

            for (int j = 0; j < temp.Count; j++)
            {
                Brave currentBrave = temp[j];

                // unitData.name 과 일치하는 Brave 탐색
                if (currentBrave != null &&
                    currentBrave.unitIdentity.name == targetName)
                {
                    Braveparty.Add(currentBrave);
                    temp.RemoveAt(j);     // 이미 사용한 Brave 제거
                    break;
                }
            }
        }

        // ② names 리스트에 없던 나머지 멤버들은 뒤쪽에 유지
        if (temp.Count > 0)
        {
            Braveparty.AddRange(temp);
        }

        if (!skipEvent)
        {
            if (gameManagerData.cModeType.Equals(GameModeType.Adventure))
            {
                currentParty = new List<Unit>(Braveparty);
                if (Braveparty.Count > 0)
                    setSelectedUnit(Braveparty[0]);

                yield return null;

                yield return invokeIE(GMEventType.AdventureUpdate);
                selectedUnit.Select();
                selectedUnit.unitUtility.collider.isTrigger = false;
            }
        }
        
        unitDataInvoke();
        yield return null;
        yield return null;
        yield return null;
    }
    public void turnEnd()
    {
        if (gameManagerData.cModeType.Equals(GameModeType.Combat))
        {
            if (devMode)
            {
                  forceNextTurn = true;
            }
            else if(gameManagerData.combatTurn.CompareTo(CombatTurn.EDelay)<0)
            {
                forceNextTurn = true;
            }
        }
    }
    
    public void turnEndEnemy()
    {
        if (gameManagerData.cModeType.Equals(GameModeType.Combat))
        {
            forceNextTurn = true;
        }
    }
    public void fallWhenAdventureStart()
    {
        if (!falling)
        {
            falling = true;
            StartCoroutine(fallWhenAdvantureIE());
        }
    }
    public IEnumerator fallWhenAdvantureIE()
    {
        if (nextStage)
        {
            int cStage = expData.stage;
            if (demoMode)
            {
                if (cStage == 2)
                {
                    SoundController.GetInst.playSubBGM(0);
                    foreach (var VARIABLE in Braveparty)
                    {
                        Destroy(VARIABLE.gameObject);
                    }

                    UIButtonFuncManager.GetInst.setNewGameLockLock(true);
                    UIButtonFuncManager.GetInst.setHaveSaveFile(false);
                    UIButtonFuncManager.GetInst.bOnTitle();
                    falling = false;
                    yield break;
                }
            }

            expData.stage = cStage + 1;
            expData.maxStage = Mathf.Max(expData.maxStage, expData.stage);
            expSaverForData.save();
            invokeExpData();
            
            nextStage = false;
            StopCoroutine(CoGameMode);
            yield return invokeIE(GMEventType.NextStage);
            yield return new WaitForFixedUpdate();
            doAdventureMode();
        }
        else
        {
            yield return invokeIE(GMEventType.Fall);
            yield return new WaitForFixedUpdate();
            foreach (var VARIABLE in Braveparty)
            {
                if (VARIABLE.tryGetUnitComponent(out AnimationManager animationManager))
                {
                    animationManager.fall = false;
                }

                VARIABLE.unitUtility.rigidbody.linearVelocity = Vector3.zero;
            }
        }
       
        
        falling = false;
    }
    public void deathUnit(Unit unit)
    {
        currentParty.Remove(unit);

        if (currentParty.Contains(selectedUnit))
        {
            gameManagerData.unitNumber = currentParty.IndexOf(selectedUnit);
        }

        if (unit is Brave brave)
        {
            Braveparty.Remove(brave);
        }
        else if(unit is Enemy enemy)
        {
            Enemyparty.Remove(enemy);
        }
        
        unitDataInvoke();
        
        if (unit.isSelected)
        {
            selectingNextUnit();
        }
        else
        {
            startCombatEnd();
        }

        try
        {
            unitDieEvent?.Invoke(unit);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    
    }
    private void unitDataInvoke()
    {
        try
        {
            unitDataManager?.Invoke(selectedUnit,Braveparty,Enemyparty,gameManagerData.unitNumber);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
    }
    private void subscriber()
    {
        unitDataManager += ActivingObjectBasis.unitSubscribe;
        addFunction(CombatTurn.Delay, () =>
        {
            unitChangePress = false;
            unitNumPress = false;
            unitNum = -1;
        },true);
    }
    IEnumerator playCombatMode()
    {
        while (true)
        {
            yield return CombatModeFunctions[gameManagerData.combatTurn].update(); // 상태패턴 적용
        }
    }
    IEnumerator playAdventureMode()
    {
        while (true)
        {
            yield return AdventureModeFunctions[gameManagerData.adventureTurn].update(); // 상태패턴 적용
        }
    }
    #region EventSubscribe
    
    public bool addFunction(GMEventType type, Action action, int priority = 10, int sequence = 10, bool once = false)
    {
        if (action == null) return false;

        List<GMSubscribeFunc> list = getList(type);
        Dictionary<Action, GMSubscribeFunc> map = getActionMap(type);

        if (map.ContainsKey(action)) return false;

        GMSubscribeFunc item = new GMSubscribeFunc(action, priority, sequence, once);

        if (run)
        {
            addToWaitList(type, item, true, true);
            return true;
        }

        binaryInsert(list, item);
        map[action] = item;
        return true;
    }


    public bool addFunction(GMEventType type, Func<IEnumerator> funcIE, int priority = 10, int sequence = 10, bool once = false)
    {
        if (funcIE == null) return false;

        List<GMSubscribeFunc> list = getList(type);
        Dictionary<Func<IEnumerator>, GMSubscribeFunc> map = getIEMap(type);

        if (map.ContainsKey(funcIE)) return false;

        GMSubscribeFunc item = new GMSubscribeFunc(funcIE, priority, sequence, once);

        if (run)
        {
            addToWaitList(type, item, true, false);
            return true;
        }

        binaryInsert(list, item);
        map[funcIE] = item;
        return true;
    }
    
    public bool removeFunction(GMEventType type, Action action)
    {
        if (action == null) return false;

        List<GMSubscribeFunc> list = getList(type);
        Dictionary<Action, GMSubscribeFunc> map = getActionMap(type);

        if (!map.TryGetValue(action, out GMSubscribeFunc item))
        {
            return false;
        }

        if (run)
        {
            addToWaitList(type, item, false, true);
            return true;
        }

        int idx = indexOf(list, item);
        if (idx >= 0) list.RemoveAt(idx);

        map.Remove(action);
        return true;
    }

    public bool removeFunction(GMEventType type, Func<IEnumerator> funcIE)
    {
        if (funcIE == null) return false;

        List<GMSubscribeFunc> list = getList(type);
        Dictionary<Func<IEnumerator>, GMSubscribeFunc> map = getIEMap(type);

        if (!map.TryGetValue(funcIE, out GMSubscribeFunc item))
        {
            return false;
        }

        if (run)
        {
            addToWaitList(type, item, false, false);
            return true;
        }

        int idx = indexOf(list, item);
        if (idx >= 0) list.RemoveAt(idx);

        map.Remove(funcIE);
        return true;
    }


    public void addFunction(CombatTurn combatTurn, Action action,bool isReady, int priority = 10, int sequence = 10, bool isOnce = false)
    {
        if (CombatModeFunctions.TryGetValue(combatTurn, out GameMode gameMode))
        {
            gameMode.addFunc(new GMSubscribeFunc(action,priority,sequence,isOnce),isReady);
        }
    }
    
    public void addFunction(CombatTurn combatTurn, Func<IEnumerator> funcIE, bool isReady, int priority = 10, int sequence = 10, bool isOnce = false)
    {
        if (funcIE == null) return;
        if (CombatModeFunctions.TryGetValue(combatTurn, out GameMode gameMode))
        {
            gameMode.addFunc(new GMSubscribeFunc(funcIE, priority, sequence, isOnce), isReady);
        }
    }
    public void addFunction(AdventureTurn adventureTurn,Action action,bool isReady, int priority = 10, int sequence = 10, bool isOnce = false)
    {
        if (AdventureModeFunctions.TryGetValue(adventureTurn, out GameMode gameMode))
        {
            gameMode.addFunc(new GMSubscribeFunc(action,priority,sequence,isOnce),isReady);
        }
    }
    
    
    public void addFunction(AdventureTurn adventureTurn, Func<IEnumerator> funcIE, bool isReady, int priority = 10, int sequence = 10, bool isOnce = false)
    {
        if (funcIE == null) return;
        if (AdventureModeFunctions.TryGetValue(adventureTurn, out GameMode gameMode))
        {
            gameMode.addFunc(new GMSubscribeFunc(funcIE, priority, sequence, isOnce), isReady);
        }
    }
    
    public void removeFunction(CombatTurn combatTurn, Action action, bool isReady)
    {
        if (CombatModeFunctions.TryGetValue(combatTurn, out GameMode gameMode))
        {
            gameMode.removeGMFunc(action,isReady);
        }
    }
    
    
    public void removeFunction(CombatTurn combatTurn, Func<IEnumerator> funcIE, bool isReady)
    {
        if (funcIE == null) return;
        if (CombatModeFunctions.TryGetValue(combatTurn, out GameMode gameMode))
        {
            gameMode.removeGMFunc(funcIE, isReady);
        }
    }
    
    public void removeFunction(AdventureTurn adventureTurn,Action action,bool isReady)
    {
        if (AdventureModeFunctions.TryGetValue(adventureTurn, out GameMode gameMode))
        {
            gameMode.removeGMFunc(action,isReady);
        }
    }
    
    public void removeFunction(AdventureTurn adventureTurn, Func<IEnumerator> funcIE, bool isReady)
    {
        if (funcIE == null) return;
        if (AdventureModeFunctions.TryGetValue(adventureTurn, out GameMode gameMode))
        {
            gameMode.removeGMFunc(funcIE, isReady);
        }
    }
    
    
    #region GMEvent
    
    private List<GMSubscribeFunc> getList(GMEventType type)
    {
        List<GMSubscribeFunc> list;
        if (!table.TryGetValue(type, out list))
        {
            list = new List<GMSubscribeFunc>(8);
            table[type] = list;
        }
        return list;
    }

    private Dictionary<Action, GMSubscribeFunc> getActionMap(GMEventType type)
    {
        Dictionary<Action, GMSubscribeFunc> map;
        if (!indexAction.TryGetValue(type, out map))
        {
            map = new Dictionary<Action, GMSubscribeFunc>();
            indexAction[type] = map;
        }
        return map;
    }

    private Dictionary<Func<IEnumerator>, GMSubscribeFunc> getIEMap(GMEventType type)
    {
        Dictionary<Func<IEnumerator>, GMSubscribeFunc> map;
        if (!indexIE.TryGetValue(type, out map))
        {
            map = new Dictionary<Func<IEnumerator>, GMSubscribeFunc>();
            indexIE[type] = map;
        }
        return map;
    }

    private void binaryInsert(List<GMSubscribeFunc> list, GMSubscribeFunc item)
    {
        // CompareTo: priority → sequence
        int lo = 0;
        int hi = list.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            // list[mid] <= item 이면 오른쪽으로
            if (list[mid].CompareTo(item) <= 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        list.Insert(lo, item);
    }

    private int indexOf(List<GMSubscribeFunc> list, GMSubscribeFunc item)
    {
        // 참조 동일성 비교
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], item))
            {
                return i;
            }
        }
        return -1;
    }

    private void removeFromActionIndex(Dictionary<Action, GMSubscribeFunc> map, GMSubscribeFunc item)
    {
        // 동일한 GMSubscribeFunc를 value로 가진 키 제거
        // (일반적으로 1:1 매핑)
        Action keyToRemove = null;
        foreach (KeyValuePair<Action, GMSubscribeFunc> kv in map)
        {
            if (ReferenceEquals(kv.Value, item))
            {
                keyToRemove = kv.Key;
                break;
            }
        }
        if (keyToRemove != null)
        {
            map.Remove(keyToRemove);
        }
    }

    private void removeFromIEIndex(Dictionary<Func<IEnumerator>, GMSubscribeFunc> map, GMSubscribeFunc item)
    {
        Func<IEnumerator> keyToRemove = null;
        foreach (KeyValuePair<Func<IEnumerator>, GMSubscribeFunc> kv in map)
        {
            if (ReferenceEquals(kv.Value, item))
            {
                keyToRemove = kv.Key;
                break;
            }
        }
        if (keyToRemove != null)
        {
            map.Remove(keyToRemove);
        }
    }

    
    #endregion
    
    #endregion
    
    public void addBrave(Brave brave)
    {
        Braveparty.Add(brave);
        brave.transform.SetParent(braveSpace);
        unitDataInvoke();
    }

    public void addEnemy(Enemy enemy)
    {
        Enemyparty.Add(enemy);
        enemy.transform.SetParent(enemySpace);
        unitDataInvoke();
    }
    
    
    public bool getUnitNum(Unit unit,out int num)
    {
        if (unit == null)
        {
            num = -1;
            return true;
        }

        switch (unit)
        {
            case Brave brave:
                num = Braveparty.IndexOf(brave);
                return true;
            case Enemy enemy:
                num = Enemyparty.IndexOf(enemy);
                return false;
            default:
                num = -1;
                return true;
        }
    }

    public Unit getUnit(int num, bool isBrave)
    {
        if (num < 0)
            return null;
        
        if (isBrave)
        {
            if (Braveparty.Count > num)
                return Braveparty[num];
        }
        else
        {
            if (Enemyparty.Count > num)
                return Enemyparty[num];
        }

        return null;
    }

    public Unit getUnit(string name)
    {
        foreach (var VARIABLE in Braveparty)
        {
            if (VARIABLE.unitIdentity.name.Equals(name))
                return VARIABLE;
        }
        foreach (var VARIABLE in Enemyparty)
        {
            if (VARIABLE.unitIdentity.name.Equals(name))
                return VARIABLE;
        }

        return null;
    }

    public List<Unit> getUnits(List<string> names)
    {
        List<Unit> result = new List<Unit>();
        foreach (var VARIABLE in names)
        {
            Unit temp = getUnit(VARIABLE);
            if (temp != null)
            {
                result.Add(temp);
            }
        }

        return result;
    }

    public List<Unit> getBraves()
    {
        return new List<Unit>(Braveparty);
    }

    public List<Unit> getEnemise()
    {
        return new List<Unit>(Enemyparty);
    }

    public List<Enemy> getEnemyParty()
    {
        return new List<Enemy>(Enemyparty);
    }

    public List<Unit> getCurrentParty()
    {
        return new List<Unit>(currentParty);
    }

    public List<string> getBraveSelectOrder()
    {
        return new List<string>(unitsData.braveSelectNum);
    }

    public void saveBraves()
    {
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE?.saveUnitData();
        }
    }
    
    #region Mode

    private Coroutine doAdvenureCo;

    public void startTitleMode()
    {
        nextStage = false;
        forceNextTurn = false;
        unitChangeLock = false;
        stopAllMode();
        setGameMode(GameModeType.Title);
        stopATurnTimer();
        StartCoroutine(invokeIE(GMEventType.Title));
    }
   
    public void doAdventureMode()
    {
        if (doAdvenureCo != null)
        {
            StopCoroutine(doAdvenureCo);
        }

        doAdvenureCo = StartCoroutine(doAdventureModeIE());
    }

    public IEnumerator doAdventureModeIE()
    {
        nextStage = false;
        forceNextTurn = false;
        yield return ScreenEffectManager.getInst.fadeToAlphaRoutine(1, 1);
        setGameMode(GameModeType.Adventure);
        if (CoGameMode !=null)
        {
            StopCoroutine(CoGameMode);
        }

        setSelectedUnit(Braveparty[0]);
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitRuntimeData.moveLock = true;
        }
        
        addFunction(AdventureTurn.Move, () =>
        {
            foreach (var VARIABLE in Braveparty)
            {
                VARIABLE.unitRuntimeData.moveLock = false;
            }

        },true,0 ,2,true) ;
        gameManagerData.adventureTurn = AdventureTurn.Load;
        gameManagerData.turnCounter = 0;
        yield return invokeIE(GMEventType.Adventure);
        startATurnTimer();
        selectedUnit.Select();
        selectedUnit.unitUtility.collider.isTrigger = false;

        if (CoGameMode != null)
        {
            StopCoroutine(CoGameMode);
        }
        CoGameMode = StartCoroutine(playAdventureMode());
        combatChanging = false;
        foreach (var VARIABLE in Braveparty)
        {
            if (VARIABLE.tryGetUnitComponent(out AnimationManager animationManager))
            {
                animationManager.fall = false;
            }

            VARIABLE.unitUtility.rigidbody.linearVelocity = Vector3.zero;
        }
    }


    /// <summary>
    /// ComBatMode로 변환해주는 함수
    /// </summary>
    public void doCombatMode()
    {
        nextStage = false;
        stopATurnTimer();
        gameManagerData.unitNumber = 0;
        gameManagerData.combatTurn = CombatTurn.Delay;
        combatChanging = false;
        currentParty = new List<Unit>(Braveparty);
        StartCoroutine(doCombatModeIE());
    }
    
    private IEnumerator doCombatModeIE()
    {
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitRuntimeData.moveLock = true;
        }
        
        forceNextTurn = false;
        setGameMode(GameModeType.Combat);
        setSelectedUnit(currentParty[gameManagerData.unitNumber]);
        changeCombatMode(gameManagerData.combatTurn);
        if (selectedUnit.tryGetUnitComponent(out MoveController moveController))
        {
            moveController.stopMove();
        }
        
        if (CoGameMode !=null)
        {
            StopCoroutine(CoGameMode);
        }

        float time = 0;

        while (FollowerManager.isFar)
        {
            yield return null;
            time += Time.deltaTime;
    
            if (time > 2)
            {
                break;
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        yield return invokeIE(GMEventType.Combat);
        yield return null;
        
        if (CoGameMode !=null)
        {
            StopCoroutine(CoGameMode);
        }
        
        CoGameMode = StartCoroutine(playCombatMode());
        
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitRuntimeData.moveLock = false;
        }

    }
    
    #region CombatMode
    
    public void changeCombatMode(CombatTurn combatTurn)
    {
        gameManagerData.combatTurn = combatTurn;

        if (gameManagerData.combatTurn == CombatTurn.Delay)
            gameManagerData.turnCounter++;
        
        try
        {
            turnCountManager?.Invoke(gameManagerData.turnCounter,gameManagerData.combatTurn);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        
        save();
    }

    public void braveHPBarUIClick(string name)
    {
        if (gameManagerData.cModeType == GameModeType.Adventure || gameManagerData.combatTurn.CompareTo(CombatTurn.EDelay)<0)
        {
            setSelectedUnit(name);
        }
    }

    public void setSelectedUnit(string name)
    {
        setSelectedUnit(getUnit(name));
    }
    
    public void setSelectedUnit(Unit unit)
    {
        if (unit != null && !unitChangeLock)
        {
            if (gameManagerData.cModeType == GameModeType.Combat)
            {
                if (selectedUnit != null && selectedUnit.gameObject != null)
                {
                    selectedUnit.unitUtility.initializeFreezeRigidbody();
                    if (selectedUnit.tryGetUnitComponent(out MoveController moveController))
                    {
                        moveController.stopMove();
                    }
                }
            }
            else if(gameManagerData.cModeType == GameModeType.Adventure)
            {
                int index = Braveparty.IndexOf(unit as Brave);

                if (index > 0 && unit.tryGetUnitComponent(out Follower unitFollower))
                {
                    Vector3 position = unit.transform.position;
                    unitFollower.teleport(Braveparty[0].transform.position);
                    for (int i = index - 1; i >= 0; i--)
                    {
                        Brave cBrave = Braveparty[i];
                        Vector3 cPosition = cBrave.transform.position;
                        if (cBrave.tryGetUnitComponent(out Follower cBraveFollower))
                        {
                            cBraveFollower.teleport(position);
                        }
                        position = cPosition;
                    }
                }

                List<Brave> party = new List<Brave>();
                party.Add(unit as Brave);
                Braveparty.Remove(unit as Brave);
                party.AddRange(Braveparty);
                Braveparty = party;
                List<string> braveNames = new();
                foreach (var VARIABLE in Braveparty)
                {
                    if (VARIABLE.tryGetUnitComponent(out ActiverManager activerManager))
                    {
                        activerManager.setObjectActive(false);
                    }

                    VARIABLE.unitUtility.setAdventureMode();
                    braveNames.Add(VARIABLE.unitIdentity.name);
                }
                
                currentParty = new List<Unit>(Braveparty);
                if (braveNames.Count == unitsData.braveParty.Count)
                {
                    unitsData.braveParty = braveNames;
                    unitsData.braveList =  new List<string>(braveNames);
                }

                UnitViewModel.GetInst.save();
            }
            
            charChange = true;
            selectedUnit = unit;
            selectedUnit.Select();
            unitDataInvoke();
            save();
        }
    }

 
    public void startBraveTurn()
    {
        if (!startCombatEnd())
        {
            gameManagerData.unitNumber = 0;
            currentParty = new List<Unit>(Braveparty);
            setSelectedUnit(currentParty[gameManagerData.unitNumber]);
        }
    }
    
    public void startEnemyTurn()
    {
        if (!startCombatEnd())
        {
            gameManagerData.unitNumber = 0;
            currentParty = new List<Unit>(Enemyparty);
        }
    }

    public bool selectUnit()
    {
        if (unitNumPress)
        {
            unitNumPress = false;
            int i = unitNum;

            if (i < 0)
            {
                return false;
            }

            if (unitsData.braveSelectNum.Count > i)
            {
                string name = unitsData.braveSelectNum[i];

                Unit unit = null;
                int index = 0;

                foreach (var brave in Braveparty)
                {
                    if (brave.unitIdentity.name.Equals(name))
                    {
                        unit = brave;
                        break;
                    }

                    index++;
                }
                
                if (unit != null)
                {
                    gameManagerData.unitNumber = index;
                    setSelectedUnit(unit);
                    return true;
                }
                return false;
            }
        }
        

        if (unitChangePress)
        {
            unitChangePress = false;
            return selectingNextUnit();
        }

        return false;
    }
    
    public bool startCombatEnd()
    {
        if (!combatChanging && !gameManagerData.cModeType.Equals(GameModeType.Title))
        {
            if (Enemyparty.Count == 0)
            {
                combatChanging = true;
                StartCoroutine(gameWin());
                addCombatCount();
                return true;
            }

            if (Braveparty.Count == 0)
            {
                combatChanging = true;
                forceNextTurn = false;
                StopCoroutine(CoGameMode);
                StartCoroutine(gameOver());
                addCombatCount();
                return true;
            }
        }

        return false;
    }
    
    public IEnumerator gameWin()
    {
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitUtility.setAdventureMode(true); // 이기자마자 죽는 버그 막아야함
        }
        yield return new WaitForSeconds(1);
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitUtility.setAdventureMode(true); // 이기자마자 죽는 버그 막아야함
           // Vector3 pos = VARIABLE.transform.position;
            //pos.y = 1.75f;
            //EffectViewModel.getInst.createEffect(EffectType.임팩트, 13, pos, quaternion.identity, false);
        }

        yield return invokeIE(GMEventType.CombatEnd);

        yield return null;
        yield return null;
        yield return null;
        
        foreach (var VARIABLE in Braveparty)
        {
            VARIABLE.unitUtility.setAdventureMode();
        }
        doAdventureMode();

        gameManagerData.combatEndHeal = true;
        save();
    }
    
    private IEnumerator healAtEndCombat()
    {
        if (gameManagerData.combatEndHeal)
        {
            yield return new WaitUntil(() => !CameraManager.GetInst.isMoving);
            yield return new WaitForSeconds(2f);
            gameManagerData.combatEndHeal = false;
            save();
            foreach (var brave in Braveparty)
            {
                SoundController.GetInst.playSFX(SFX.etc, brave.transform.position, 2);
                EffectViewModel.getInst.createEffectInHitUnit(brave, 14);
                brave.unitRuntimeData.isDead = false;
                brave.getUnitComponent<UnitCombatStatuses>().cleanse();
                float heal = brave.getUnitComponent<UnitCombatResources>().receiveHeal(null, -1, (int)(brave.combatAttributes.MaxHp / 3), false);
                FloatingTextManager.GetInst.addFloatingText(brave, new FTHeal(heal));

                brave.saveUnitData();
            }
        }
    }

    public void healAtRestRoom()
    {
        foreach (var VARIABLE in Braveparty)
        {
            SoundController.GetInst.playSFX(SFX.etc,VARIABLE.transform.position,2);
            EffectViewModel.getInst.createEffectInHitUnit(VARIABLE, 14);
            if (VARIABLE.tryGetUnitComponent(out UnitComponents.Skill skill))
            {
                skill.resetCoolTime();
            }

            float heal = VARIABLE.getUnitComponent<UnitCombatResources>().receiveHeal(null,-1,(int)VARIABLE.combatAttributes.MaxHp,false);
            FloatingTextManager.GetInst.addFloatingText(VARIABLE, new FTHeal(heal));

            VARIABLE.saveUnitData();
        }
    }
    
    public IEnumerator gameOver()
    {
        yield return new WaitForSeconds(2.5f);
        yield return invokeIE(GMEventType.Gameover);
    }
    public bool selectingNextUnit()
    {
        if (gameManagerData.cModeType == GameModeType.Combat)
        {
            if (startCombatEnd())
            {
                return true;
            }
        }

        if (selectedUnit is Brave)
        {
            int cIndex = unitsData.braveSelectNum.IndexOf(selectedUnit.unitIdentity.name);

            if (cIndex < 0)
            {
                return false;
            }

            int selectCount = unitsData.braveSelectNum.Count;
            bool isPreviousSelect = gameManagerData.cModeType == GameModeType.Adventure;
            
            for (int i = 0; i < selectCount; i++)
            {
                if (isPreviousSelect)
                {
                    if (--cIndex < 0)
                    {
                        cIndex = selectCount - 1;
                    }
                }
                else
                {
                    if (++cIndex >= selectCount)
                    {
                        cIndex = 0;
                    }
                }

                string cName = unitsData.braveSelectNum[cIndex];

                foreach (var unit in currentParty)
                {
                    if (unit == null)
                    {
                        continue;
                    }
                
                    if (unit.unitIdentity.name.Equals(cName))
                    {
                        gameManagerData.unitNumber = currentParty.IndexOf(unit);
                        setSelectedUnit(unit);
                        return true;
                    }
                }
            }
        }
        else
        {
            Unit unit = null;
            gameManagerData.unitNumber++;

            if (gameManagerData.unitNumber < currentParty.Count)
            {
                unit = currentParty[gameManagerData.unitNumber];
            }
            else
            {
                gameManagerData.unitNumber = 0;
                unit = currentParty[0];
            }

            if (unit != null)
            {
                setSelectedUnit(unit);
                return true;
            }
        }

       

        return false;
    }
    
    public bool checkTurnEnd()
    {
        foreach (var VARIABLE in currentParty)
        {
            if(VARIABLE != null)
                if (!VARIABLE.unitUtility.isDone())
                    return false;
        }

        return true;
    }
    
    #endregion
    
    public void changeAdventureMode(AdventureTurn adventureTurn)
    {
        gameManagerData.adventureTurn = adventureTurn;
    }
    

    #endregion
    

    private void stopATurnTimer()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }
    }


    private void startATurnTimer()
    {
        stopATurnTimer();

        if (expData.combatCount > 0)
        {
            coroutine = StartCoroutine(startATurnTimerIE());
        }
    }
    private IEnumerator startATurnTimerIE()
    {
        while (true)
        {
            yield return new WaitForSeconds(adventureTurnDist);
            changeCombatMode(CombatTurn.Delay);
            StartCoroutine(invokeIE(GMEventType.AT_Brave));
            changeCombatMode(CombatTurn.EDelay);
            StartCoroutine(invokeIE(GMEventType.AT_Enemy));
        }
    }
    public void clearAllEvents()
    {
        turnCountManager = null;
        unitDataManager = null;
        gamemodeManager = null;
        unitDieEvent = null;
    }

    public void ReceiveInitialData(GameModeFlag initialData)
    {
        demoMode = (initialData & GameModeFlag.Demo) != 0;
        devMode = (initialData & GameModeFlag.Dev) != 0;
    }
}
