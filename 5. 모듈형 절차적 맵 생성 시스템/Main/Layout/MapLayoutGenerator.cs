using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MapBuild
{
    /// <summary>
    /// 스테이지/레이아웃 옵션(MapLayoutStageOption, MapLayoutOption)을 기반으로
    /// MapLayoutData(방/길/보스/회복/시크릿 등으로 구성된 맵 그래프)를 절차적으로 생성하는 클래스.
    /// 
    /// 생성 방식은 WaitData 큐(BFS)로 확장 후보를 관리하고,
    /// RouteSolver가 만든 경로 프린트(RoutePrinterData)를 buildRoute로 실제 엔트리로 변환한다.
    /// ForkCounter로 depth별 분기(Fork) 횟수를 제한하며,
    /// 옵션에 따라 SecretRoad/SecretRoom/RestRoom/BossRoom 배치 규칙을 적용한다.
    /// </summary>
    public class MapLayoutGenerator : MonoBehaviour
    {
        [SerializeField] private List<MapLayoutStageOption> options = new();
        [SerializeField] private MapLayoutSortInfo cMapLayoutSortInfo;
        [SerializeField] private MapLayoutOption _layoutOption = null;
        [SerializeField] private TextAsset defaultMapData = null;
        [SerializeField] private int cSeed;

        public MapLayoutStageOption cStageOption { get; private set; } //인게임내 난이도 조절 관련 옵션

        public MapLayoutOption layoutOption
        {
            get { return _layoutOption; }
        } // 맵 형태 관련 옵션

        public RandomUtility randomUtility { get; private set; }
        public MapLayoutData cData { get; private set; }
        public Map fallbackMap { get; private set; }

        private ForkCounter forkCounter = new ForkCounter();

        private RouteSolver routeSolver;

        private Queue<WaitData> waitDatas = new Queue<WaitData>();

        Vector2Int[] allDirs = new Vector2Int[4]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private void Awake()
        {
            routeSolver = new RouteSolver(this, allDirs);
        }

        public void setCOption(int floor)
        {
            // floor는 1부터 시작하는 값(1-based index)으로 전달된다고 가정한다.
            // 내부 options 리스트는 0-based index이므로 -1 보정한다.
            floor -= 1;

            if (options == null || options.Count == 0)
            {
                throw new InvalidOperationException("Map Layout Stage Option이 등록되지 않았습니다.");
            }

            floor = Mathf.Clamp(floor, 0, options.Count - 1);

            MapLayoutStageOption stageOption = options[floor];

            // 해당 인덱스가 null이면 0번으로 fallback
            if (stageOption == null)
            {
                stageOption = options[0];
            }

            // 그래도 0번이 null일 수 있으니 최종 방어
            if (stageOption == null)
            {
                throw new InvalidOperationException("사용 가능한 Map Layout Stage Option이 없습니다.");
            }

            cStageOption = stageOption;
            cSeed = cStageOption.getSeedFromOption();
            randomUtility = new RandomUtility(cSeed);
        }

        /// <summary>
        /// MapLayoutData를 절차적으로 생성한다.
        /// 호출 전제: makeMapData() 실행 전에 반드시 setCOption(int floor)을 먼저 호출해야 한다.
        /// 목표 전투방 수(combatRoomCount)를 만족하도록 최대 1000번 생성 시도하며,
        /// 모두 실패하면 인스펙터에 등록된 기본 맵 데이터를 사용한다.
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        public IEnumerator makeMapData()
        {
            if (cStageOption == null || randomUtility == null)
            {
                throw new InvalidOperationException("setCOption()을 먼저 호출해야 합니다.");
            }

            fallbackMap = null;

            for (int i = 0; i < 1000; i++)
            {
                WaitData firstWaitData = createStartBase();

                buildMap(firstWaitData);

                cMapLayoutSortInfo = cData.getMapInfoCounter();

                if (cMapLayoutSortInfo.getCount(MLDEntryType.CombatRoom) +
                    cMapLayoutSortInfo.getCount(MLDEntryType.BossRoom) >= cStageOption.combatRoomCount)
                {
                    yield break;
                }
            }

            cData = null;

            if (defaultMapData == null)
            {
                Debug.LogError("Default map data is not assigned.");
                yield break;
            }

            fallbackMap = JsonUtility.FromJson<Map>(defaultMapData.text);

            if (fallbackMap == null)
            {
                Debug.LogError("Default map data could not be deserialized.");
            }
        }

        private WaitData createStartBase()
        {
            cData = new MapLayoutData(cSeed);

            MapLayoutDataEntry startPoint = cData.addNewEntry(MLDEntryType.StartPoint, Vector2Int.zero, 0);
            MapLayoutDataEntry beforeEntry = startPoint;
            Vector2Int startpos = startPoint.pos;
            Vector2Int dir = allDirs[randomUtility.getRandomValue(0, allDirs.Length - 1)];
            int count = layoutOption.minRoomToRoomRoadLength;

            cData.startPoint = startPoint;

            for (int i = 1; i <= count; i++)
            {
                MapLayoutDataEntry entry = cData.addNewEntry(MLDEntryType.Road, startpos + dir * i, 0);
                beforeEntry.addNext(entry);
                beforeEntry = entry;
            }

            Vector2Int destination = startpos + dir * (count + 1);

            MapLayoutDataEntry firstCombat = cData.addNewEntry(MLDEntryType.CombatRoom, destination, 0);
            beforeEntry.addNext(firstCombat);

            WaitData firstWaitData = new WaitData(firstCombat);
            firstWaitData.dir = dir;
            firstWaitData.depth = 1;


            return firstWaitData;
        }

        private void buildMap(WaitData input)
        {
            if (input == null)
            {
                return;
            }

            forkCounter.initialize();
            waitDatas.Clear();
            waitDatas.Enqueue(input);

            int roomCount = 0;
            int routeIndex = 0;
            bool first = true;

            while (waitDatas.Count > 0 && roomCount < cStageOption.combatRoomCount)
            {
                WaitData cWaitData = waitDatas.Dequeue();
                int forkCount = forkCounter.getFork(cWaitData.routeIndex, cWaitData.depth);

                if (forkCount >= layoutOption.maxForkOnDepth)
                {
                    continue;
                }

                if (routeSolver.tryBuildRoutePrinters(cWaitData, out List<RoutePrinterData> routePrinters))
                {
                    List<WaitData> result = buildRoute(cWaitData, routePrinters, routeIndex, out int cRoomCount);
                    roomCount += cRoomCount;
                    inputWaitData(result, first);
                    routeIndex++;
                    forkCounter.addFork(cWaitData.routeIndex, cWaitData.depth, 1);
                }

                first = false;
            }
        }

        private void inputWaitData(List<WaitData> waitDataList, bool first)
        {
            foreach (var waitData in waitDataList)
            {
                // 마지막 Depth에서 Fork 생성 불가 옵션이면 스킵
                if (waitData.isLastDepth && !layoutOption.canMakeForkOnLastDepth)
                {
                    continue;
                }

                // Fork Depth에서 Fork 생성 불가 옵션이고, 첫 호출이 아니면 스킵
                if (waitData.isFork && !layoutOption.canMakeForkOnForkDepth && !first)
                {
                    continue;
                }

                // 방향 결정
                if (!tryGetRandomDir(waitData.neighborDir, out waitData.dir))
                {
                    continue;
                }

                // 깊이가 제한을 넘으면 이후 로직 없음 (원래도 depth 조건 밖이면 아무것도 안 함)
                if (waitData.depth > cStageOption.routeRoomCountsMax)
                {
                    continue;
                }

                // CombatRoom 처리
                if (waitData.entry.type == MLDEntryType.CombatRoom)
                {
                    if (randomUtility.rollChance(layoutOption.pSecretRoadFromRoom) &&
                        waitData.startGift <= cStageOption.maxGift
                        && !waitData.haveSecretRoom)
                    {
                        waitData.isSecret = true;
                        waitData.startGift++;
                        waitDatas.Enqueue(waitData);
                    }

                    continue;
                }

                // 이하: CombatRoom이 아닌 경우 처리

                // 비밀방을 이미 갖고 있는데, 비밀방에서 길 분기 불가면 스킵
                if (waitData.haveSecretRoom && !layoutOption.canMakeForkAtSecretRoomRoad)
                {
                    continue;
                }

                // 현재 엔트리가 SecretRoad인데, SecretRoad에서 Fork 불가면 스킵
                if (waitData.entry.type == MLDEntryType.SecretRoad && !layoutOption.canMakeForkAtSecretRoad)
                {
                    continue;
                }

                // 같은 depth에서 이미 SecretRoom이 있고 onlyOneSecret이면 시크릿로드는 생성 불가능
                if (waitData.haveSecretRoomInDepth && layoutOption.onlyOneSecret)
                {
                    waitDatas.Enqueue(waitData);
                    continue;
                }

                // Secret 판정(원래 로직 그대로: RestRoom이면 isSecret 세팅/증가 안 함)
                if (randomUtility.rollChance(layoutOption.pSecretRoad) &&
                    waitData.startGift <= cStageOption.maxGift)
                {
                    if (waitData.entry.type != MLDEntryType.RestRoom)
                    {
                        waitData.isSecret = true;
                        waitData.startGift++;
                    }
                }

                // Enqueue는 위 조건들 통과 시 항상 수행 (원래 중첩에서도 동일)
                waitDatas.Enqueue(waitData);
            }
        }

        private List<WaitData> buildRoute(WaitData waitData, List<RoutePrinterData> routePrinters, int routeIndex,
            out int RoomCount)
        {
            List<WaitData> result = new List<WaitData>();

            RoomCount = 0;
            int cDepth = waitData.depth;
            int startGift = waitData.startGift;
            int startRecover = waitData.startRecover;
            int lastRecoverDepth = waitData.lastRecoverDepth;
            int recoverDepth = randomUtility.getRandomValue(cStageOption.minRecoverDepth, cStageOption.maxRecoverDepth);
            bool canMakeRecover = randomUtility.rollChance(cStageOption.pRecoverInDepthRange) && startRecover == 0;

            List<WaitData> cDepthWaitData = new();
            WaitData beforeWaitData = waitData;

            int printerCount = routePrinters.Count;

            for (int d = 0; d < printerCount; d++)
            {
                RoutePrinterData cRoutePrinterData = routePrinters[d];

                if (cRoutePrinterData == null || cRoutePrinterData.mask == null || cRoutePrinterData.mask.Count < 2)
                {
                    continue;
                }

                List<Vector2Int> mask = cRoutePrinterData.mask;
                Vector2Int startPos = cRoutePrinterData.pos;
                Vector2Int recoverPos = startPos;
                Vector2Int before = startPos;

                int maskCount = mask.Count;

                if (maskCount > 1)
                {
                    recoverPos = startPos + mask[randomUtility.getRandomValue(1, mask.Count - 2)];
                }

                bool haveSecretRoom = false;
                bool isLastDepth = d == printerCount - 1;
                bool isFork = d == 0;
                bool isLastCombat = d == printerCount - 2;
                bool isBossRoom = randomUtility.rollChance(cStageOption.pBossRoom);
                bool lastRecover = isLastCombat && randomUtility.rollChance(cStageOption.pRecoverBeforeLastCombat);
                bool makeRecover = lastRecover && isBossRoom || (canMakeRecover && recoverDepth == cDepth);

                int roadCount = 0;

                makeRecover &= cDepth - lastRecoverDepth >= 2; //연속 배치 방지
                makeRecover &=
                    !recoverPos.Equals(cRoutePrinterData
                        .secretBeforePos); // 시크릿 방 연결 지점(secretBeforePos)에는 회복방을 배치하지 않음 (겹침 방지)

                for (int i = 0; i < maskCount; i++)
                {
                    bool isRoom = i == maskCount - 1;
                    Vector2Int maskPos = mask[i];
                    Vector2Int installPos = startPos + maskPos;
                    Vector2Int beforeDir = installPos - before;
                    before = installPos;
                    MLDEntryType entryType;

                    MapLayoutDataEntry newEntry = cData.addNewEntry(MLDEntryType.Road, installPos, routeIndex);
                    WaitData newWaitData = new WaitData(newEntry);
                    newWaitData.neighborDir.Add(beforeDir);
                    beforeWaitData.neighborDir.Add(-beforeDir);
                    beforeWaitData.entry.addNext(newEntry);
                    beforeWaitData = newWaitData;

                    cDepthWaitData.Add(newWaitData);

                    if (isRoom)
                    {
                        if (isLastDepth)
                        {
                            entryType = MLDEntryType.EndPoint;
                        }
                        else
                        {
                            entryType = MLDEntryType.CombatRoom;

                            if (isLastCombat && isBossRoom)
                            {
                                entryType = MLDEntryType.BossRoom;
                            }

                            RoomCount++;
                        }

                        cDepth++;
                        roadCount = 0;
                    }
                    else
                    {
                        if (makeRecover && recoverPos.Equals(installPos))
                        {
                            entryType = MLDEntryType.RestRoom;
                            startRecover++;
                            canMakeRecover = false;
                            lastRecoverDepth = cDepth;
                        }
                        else
                        {
                            if (waitData.isSecret && isFork)
                            {
                                entryType = MLDEntryType.SecretRoad;
                            }
                            else
                            {
                                entryType = MLDEntryType.Road;
                            }
                        }

                        roadCount++;
                    }

                    if (cRoutePrinterData.installSecretRoom && installPos == cRoutePrinterData.secretBeforePos)
                    {
                        MapLayoutDataEntry secretRoom = cData.addNewEntry(MLDEntryType.SecretRoom,
                            cRoutePrinterData.secretPos, routeIndex);
                        secretRoom.depth = cDepth;
                        newEntry.addNext(secretRoom);
                        startGift++;
                        haveSecretRoom = true;
                        newWaitData.haveSecretRoom = true;
                    }

                    newEntry.type = entryType;
                    newEntry.depth = cDepth;

                    newWaitData.isFork = isFork;
                    newWaitData.isLastDepth = isLastDepth;
                    newWaitData.roadCount = roadCount;
                    newWaitData.lastRecoverDepth = lastRecoverDepth;
                    newWaitData.routeIndex = routeIndex;
                    newWaitData.depth = cDepth;
                    newWaitData.startGift = startGift;
                    newWaitData.startRecover = startRecover;
                }

                if (cDepthWaitData.Count > 0)
                {
                    randomUtility.shuffle(cDepthWaitData);
                    foreach (var data in cDepthWaitData)
                    {
                        data.haveSecretRoomInDepth = haveSecretRoom;
                        result.Add(data);
                    }

                    cDepthWaitData.Clear();
                }
            }

            return result;
        }

        private bool tryGetRandomDir(List<Vector2Int> excludedDirs, out Vector2Int result)
        {
            List<Vector2Int> candidateDirs = new List<Vector2Int>(4);

            for (int i = 0; i < allDirs.Length; i++)
            {
                Vector2Int dir = allDirs[i];

                if (excludedDirs.Contains(dir))
                {
                    continue;
                }

                candidateDirs.Add(dir);
            }

            // 전부 제외된 경우: 더 갈 곳이 없는 상태
            if (candidateDirs.Count == 0)
            {
                result = Vector2Int.zero;
                return false;
            }

            int dirIndex = randomUtility.getRandomValue(0, candidateDirs.Count - 1);
            result = candidateDirs[dirIndex];
            return true;
        }
    }
}
