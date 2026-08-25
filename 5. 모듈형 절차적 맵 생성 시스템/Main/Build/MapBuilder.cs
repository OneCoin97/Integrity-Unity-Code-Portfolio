using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
{
    /// <summary>
    /// MapLayoutData(노드 기반 레이아웃 데이터)를 입력으로 받아,
    /// RoadBuilder와 RoomBuilder를 사용해 실제 블록 맵(Map)을 구성하는 빌드 파이프라인 클래스.
    ///
    /// - 레이아웃 노드를 Stack 기반 DFS 방식으로 순회하며
    ///   길(Road), 방(Room), 분기(Fork), 시크릿 경로 등을 순차적으로 설치한다.
    /// - MapPartSet을 통해 스테이지별 파츠(길/방)를 랜덤 선택한다.
    /// - BuildEntry를 통해 현재 빌드 상태(위치, 사용된 파츠, 길이 누적 등)를 관리한다.
    /// - BlockMapManager와 Map을 갱신하여 최종 맵 데이터를 생성한다.
    ///
    /// 이 클래스는 "레이아웃 해석 및 조립 오케스트레이터" 역할을 담당하며,
    /// 실제 블록 배치는 RoadBuilder / RoomBuilder가 수행한다.
    /// </summary>
    [RequireComponent(typeof(RoadBuilder), typeof(RoomBuilder))]
    public class MapBuilder : MonoBehaviour
    {
        private RoadBuilder roadBuilder;
        private RoomBuilder roomBuilder;
        private BlockMapManager blockMapManager = new ();
        private Random rng;
        
        [SerializeField]
        private MapBuilderOption option = new ();
        [SerializeField]
        private List<MapPartSetSO> mapPartSetList = new();
        
        private Map cMap;
        private MapLayoutData layoutData;
        private MapPartSet mapPartSet;
        private Stack<BuildEntry> stack = new ();
        
        [SerializeField]
        private Vector2Int cPos = new Vector2Int(0, 0);
        [SerializeField]
        private Vector2Int forkPos = new Vector2Int(0, 0);
        private BuildEntry baseEntry;
        private int PocketCount;
        
        private void Awake()
        {
            roadBuilder = GetComponent<RoadBuilder>();
            roomBuilder = GetComponent<RoomBuilder>();
        }
        
        public void setLayoutData(MapLayoutData layoutData,int stage)
        {
            if (layoutData == null)
            {
                throw new ArgumentNullException(nameof(layoutData));
            }

            this.layoutData = layoutData;
            rng = new Random(layoutData.seed);
            cMap = new Map();
            cMap.seed = layoutData.seed;
            cPos = Vector2Int.zero;
            forkPos = Vector2Int.zero;

            setMapPart(stage);
            blockMapManager.initialize();
            roomBuilder.initialize(blockMapManager,cMap);
            roadBuilder.initialize(blockMapManager,cMap);
            stack.Clear();
            
            if (this.layoutData != null && this.layoutData.startPoint != null && this.layoutData.startPoint.next0 != null)
            {
                Dir dir = this.layoutData.startPoint.next0.getDir();
                PocketCount = option.pocketCount;
                RoomPartRandom roomPartRandom = mapPartSet.getRandomRoom(MLDEntryType.StartPoint);
                roomBuilder.setData(roomPartRandom);
                Vector2Int startPos = DirUtility.getDirVector(dir) * (roomPartRandom.main.getSizeX() / 2-1);
                blockMapManager.startPos = new Vector3Int(startPos.x, 0, startPos.y);
                cPos = roomBuilder.buildRoom(cPos, dir);
                BuildEntry buildEntry = new BuildEntry(cPos, this.layoutData.startPoint.next0);
                buildEntry.cRoadPart = mapPartSet.getDefaultRoad();
                stack.Push(buildEntry);
            }
        }
        
        private void setMapPart(int stage)
        {
            stage -= 1;

            if (mapPartSetList == null || mapPartSetList.Count == 0)
            {
                throw new InvalidOperationException("Map Part Set이 등록되지 않았습니다.");
            }

            int index = stage >= 0 && stage < mapPartSetList.Count ? stage : 0;
            MapPartSetSO selectedPartSet = mapPartSetList[index];

            if (selectedPartSet == null)
            {
                throw new InvalidOperationException($"Map Part Set {index}번 항목이 비어 있습니다.");
            }

            mapPartSet = selectedPartSet.getData(rng);

            if (mapPartSet == null)
            {
                throw new InvalidOperationException($"Map Part Set {index}번 데이터를 생성하지 못했습니다.");
            }
        }
        
        public Map getMap()
        {
            Vector3Int mapSize = blockMapManager.findMapSize(out Vector3Int bottomLeft);
            cMap.setBlockMap(blockMapManager.makingBlockMap("test", mapSize, bottomLeft), bottomLeft);

            return cMap;
        }

        #region Pipeline
        public IEnumerator startMakingMap()
        {
            while (stack.Count > 0)
            {
                baseEntry = stack.Pop();
                cPos = baseEntry.start;

                MapLayoutDataEntry start = baseEntry.data;
                MapLayoutDataEntry destination = getDestination(start, out int count);

                bool fixedRoad = count < 2 || baseEntry.fixedRoad;

                roadBuilder.setData(baseEntry.cRoadPart);
                buildRoadUpToDestination(start, destination, fixedRoad);

                if (isRoom(destination.type))
                {
                    RoomPartRandom cRoomPart = getRoomPart(destination.type,start);
                    buildRoadToRoom(destination, cRoomPart);
                    installRoom(destination,cRoomPart);
                }
                else
                {
                    if (destination.getForwardEntry() == null)
                    {
                        buildForkTJunction(destination);
                    }
                    else
                    {
                        buildForkSideJunction(destination);
                    }
                }

                yield return null;
            }
        }

        private void buildRoadUpToDestination(MapLayoutDataEntry start,MapLayoutDataEntry destination,bool fixedRoad)
        {
            while (true)
            {
                Dir cDir;
                int restLength;
                MapLayoutDataEntry next;
                
                if (!fixedRoad && tryChangeRoadPart(out RoadPartRandom beforeRoadPart))
                {
                    start = makeEndBridge(start,beforeRoadPart);
                }
                
                cDir = start.getDir();
                start = findCurve(start);
                
                if (start.Equals(destination))
                {
                    return;
                }
                
                next = start.next0;
                if (next == null) //진행 가능한 길이 더이상 없는 상태
                {
                    return;
                }
                
                restLength = DirUtility.getRestLength(cDir, cPos, getPos(start.pos));
                cPos = roadBuilder.buildForkWithDir(cPos, cDir, next.getDir(), restLength, baseEntry.consumeStartBridge()).Item2;
                baseEntry.installedRoadLength += restLength;
                
                if (next.Equals(destination))
                {
                    return;
                }
                
                start = next;
            }
        }
        
        private void buildRoadToRoom(MapLayoutDataEntry destination,RoomPartRandom cRoomPart)
        {
            Vector2Int roomOffset = Vector2Int.zero;
            Dir dir = destination.getDir();
            int restLength;

            if (baseEntry.roomDistanceMode)
            {
                restLength = baseEntry.roomDistance;
            }
            else
            {
                restLength = DirUtility.getRestLength(dir, cPos, getPos(destination.pos));
                restLength -= cRoomPart.getCenterLength()+1;
            }
            
            baseEntry.installedRoadLength += restLength;

            
            if ((destination.type == MLDEntryType.CombatRoom || destination.type == MLDEntryType.BossRoom) &&cRoomPart.haveRoomOffset(out int entranceOffset))
            {
                
                Dir offsetDir = entranceOffset < 0? DirUtility.getLeftDir(dir):DirUtility.getReverseDir(DirUtility.getLeftDir(dir));
                entranceOffset = Mathf.Abs(entranceOffset);
                Vector2Int tempPos = cPos;
                
                cPos = roadBuilder.buildForkWithDir(cPos, dir,offsetDir,restLength/2, baseEntry.consumeStartBridge()).Item2;
                cPos = roadBuilder.buildForkWithDir(cPos, offsetDir,dir,entranceOffset, false).Item2;
                
                if (entranceOffset <= baseEntry.cRoadPart.fork.getSizeX())
                {
                    int length = Mathf.Clamp(restLength / 2 - baseEntry.cRoadPart.main.getSizeZ()/2+1, 0, int.MaxValue);
                    roadBuilder.buildRoad(tempPos , dir,length, false,false);
                }
                
                roomOffset -= DirUtility.getDirVector(offsetDir)*entranceOffset;
                restLength -= restLength / 2;
            }
            
            cPos = roadBuilder.buildRoad(cPos, dir,restLength, baseEntry.consumeStartBridge(),true,getPocketCount(destination));
            
            if (baseEntry.makeSkillUpgrade)
            {
                blockMapManager.makeSkillUpgrade(cPos - DirUtility.getDirVector(dir) * restLength / 2);
            }
            
            cPos += roomOffset - DirUtility.getDirVector(dir);
        }

        private void installRoom(MapLayoutDataEntry destination, RoomPartRandom cRoomPart)
        {
            MapLayoutDataEntry curve = destination.getCurveEntry();
            MapLayoutDataEntry next = destination.getForwardEntry();
            Dir dir = destination.getDir();
            
             switch (destination.type)
            {
                case MLDEntryType.EndPoint:
                case MLDEntryType.SecretRoom:
                {
                    cPos = roomBuilder.buildRoom(cPos, dir);
                }
                    break;
                case MLDEntryType.BossRoom:
                case MLDEntryType.CombatRoom:
                {
                    if (curve != null)
                    {
                        setPos(roomBuilder.buildCombatRoomWithSecret(cPos, dir,curve.getDir()));

                        if (tryPush(forkPos, curve, out BuildEntry buildEntry))
                        {
                            RoomSecretExit roomSecretExit = cRoomPart.getSecretExit(dir, curve.getDir());
                            RoadPartRandom secretRoadPart = roomSecretExit?.getSecretRoadPartFromRoom(rng);

                            buildEntry.fixedRoad = true;

                            if (secretRoadPart != null)
                            {
                                buildEntry.cRoadPart = secretRoadPart;
                                if (curve.type == MLDEntryType.SecretRoom)
                                {
                                    buildEntry.setRoomDistance(roomSecretExit.roomDistance);
                                }
                                else
                                {
                                    buildEntry.makeSkillUpgrade = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        cPos = roomBuilder.buildRoom(cPos, dir);
                    }
                }
                    break;
                case MLDEntryType.RestRoom:
                {
                    if (curve != null)
                    {
                        if (next == null)
                        {
                            (Vector2Int, Vector2Int) posTuple = roomBuilder.buildRoomNoForward(cPos, dir);
                            (MapLayoutDataEntry, MapLayoutDataEntry) entryTuple = getLeftRightEntries(destination);
                           
                            pushRoom(posTuple.Item1,entryTuple.Item1);
                            pushRoom(posTuple.Item2,entryTuple.Item2);
                            
                        }
                        else
                        {
                            setPos(roomBuilder.buildRoom(cPos, dir,curve.getDir()));
                            pushRoom(cPos,next);
                            pushRoom(forkPos,curve);
                        }
                    }
                    else
                    {
                        cPos = roomBuilder.buildRoom(cPos, dir);
                        pushRoom(cPos,next);
                    }
                    
                    return;
                }
            }

            pushRoom(cPos, next);
        }
        
        private void buildForkTJunction(MapLayoutDataEntry destination)
        {
            Dir cDir = destination.getDir();
            int restLength = DirUtility.getRestLength(cDir, cPos, getPos(destination.pos));
            baseEntry.installedRoadLength += restLength;
            bool startBridge = baseEntry.consumeStartBridge();
            (MapLayoutDataEntry, MapLayoutDataEntry) result = getLeftRightEntries(destination);
            MapLayoutDataEntry leftCurveEntry = result.Item1;
            MapLayoutDataEntry rightCurveEntry = result.Item2;
                
            if (leftCurveEntry.type == MLDEntryType.SecretRoad || leftCurveEntry.type == MLDEntryType.SecretRoom || 
                rightCurveEntry.type == MLDEntryType.SecretRoad || rightCurveEntry.type == MLDEntryType.SecretRoom)
            {
                MapLayoutDataEntry secretEntry;
                MapLayoutDataEntry normal;
                if (leftCurveEntry.type == MLDEntryType.SecretRoad ||
                    leftCurveEntry.type == MLDEntryType.SecretRoom)
                {
                    secretEntry = leftCurveEntry;
                    normal = rightCurveEntry;
                }
                else
                {
                    normal = leftCurveEntry;
                    secretEntry = rightCurveEntry;
                }
                    
                (Vector2Int,Vector2Int) posTuple = roadBuilder.buildSecretRoad(cPos, cDir, secretEntry.getDir(), restLength-option.cellHalfRange, startBridge);
                mapPartSet.updateSecretRoad(baseEntry.cRoadPart);
                Vector2Int normalPos = roadBuilder.buildForkWithDir(posTuple.Item1, cDir, normal.getDir(), option.cellHalfRange, false).Item2;
                    
                pushSecret(posTuple.Item2,secretEntry);
                push(normalPos,normal);
            }
            else
            {
                (Vector2Int,Vector2Int) posTuple = roadBuilder.buildForkLeftRight(cPos, cDir, restLength, startBridge);
                push(posTuple.Item1,leftCurveEntry);
                push(posTuple.Item2,rightCurveEntry);
            }
            
            if (baseEntry.makeSkillUpgrade)
            {
                blockMapManager.makeSkillUpgrade(cPos + DirUtility.getDirVector(cDir) * restLength / 2);
            }
        }
        
        private void buildForkSideJunction(MapLayoutDataEntry destination)
        {
            Dir cDir = destination.getDir();
            MapLayoutDataEntry forwardEntry = destination.getForwardEntry();
            MapLayoutDataEntry curveEntry = destination.getCurveEntry();
            int restLength = DirUtility.getRestLength(cDir, cPos, getPos(destination.pos));
            baseEntry.installedRoadLength += restLength;
            bool startBridge = baseEntry.consumeStartBridge();
            
            if (curveEntry.type == MLDEntryType.SecretRoad || curveEntry.type == MLDEntryType.SecretRoom)
            {
                setPos(roadBuilder.buildSecretRoad(cPos, cDir, curveEntry.getDir(), restLength, startBridge));
                mapPartSet.updateSecretRoad(baseEntry.cRoadPart);
                
                push(cPos,forwardEntry);
                pushSecret(forkPos,curveEntry);
            }
            else
            {
                setPos(roadBuilder.buildForkWithDir(cPos, cDir, curveEntry.getDir(), restLength, startBridge));

                if (forwardEntry.type == MLDEntryType.SecretRoad || forwardEntry.type == MLDEntryType.SecretRoom)
                {
                    (Vector2Int, Vector2Int) posTuple = roadBuilder.buildSecretRoad(forkPos, curveEntry.getDir(),
                        cDir, 0,false);
                    mapPartSet.updateSecretRoad(baseEntry.cRoadPart);
                    cPos = posTuple.Item2;
                    forkPos = posTuple.Item1;
                    
                    pushSecret(cPos,forwardEntry);
                    push(forkPos,curveEntry);
                }
                else
                {
                    push(cPos,forwardEntry);
                    push(forkPos,curveEntry);
                }
            }
            
            if (baseEntry.makeSkillUpgrade)
            {
                blockMapManager.makeSkillUpgrade(cPos + DirUtility.getDirVector(cDir) * restLength / 2);
            }
        }
        
        #endregion
        
        private MapLayoutDataEntry makeEndBridge(MapLayoutDataEntry start,RoadPartRandom beforeRoadPart)
        {
            if (!baseEntry.startAtRoom)
            {
                if (beforeRoadPart.bridge != null)
                {
                    Dir cDir = start.getDir();
                    int restLength = DirUtility.getRestLength(cDir, cPos, getPos(start.pos));
                    MapLayoutDataEntry next = start.next0;
                    int sizeX = beforeRoadPart.bridge.getSizeX() + 1;
                    
                    if (start != findCurve(start))
                    {
                        roadBuilder.setData(beforeRoadPart);
                        cPos = roadBuilder.buildRoad(cPos, cDir, restLength, false, true);
                        start = next;
                    }
                    else if(restLength >= sizeX)
                    {
                        roadBuilder.setData(beforeRoadPart);
                        cPos = roadBuilder.buildRoad(cPos, cDir, sizeX, false, true);
                        start = next;
                    }
                }
                
                roadBuilder.setData(baseEntry.cRoadPart);
            }
        
            return start;
        }
        
        private (MapLayoutDataEntry, MapLayoutDataEntry) getLeftRightEntries(MapLayoutDataEntry destination)
        {
            if (destination == null)
            {
                return (null,null);
            }

            MapLayoutDataEntry leftCurveEntry;
            MapLayoutDataEntry rightCurveEntry;
            
            Dir left = DirUtility.getLeftDir(destination.getDir());
            MapLayoutDataEntry curveEntry = destination.getCurveEntry();
            
            if (curveEntry == null)
            {
                return (null,null);
            }

            if (curveEntry.getDir().Equals(left))
            {
                leftCurveEntry = curveEntry;
                rightCurveEntry = destination.getOtherEntry(curveEntry);
            }
            else
            {
                rightCurveEntry = curveEntry;
                leftCurveEntry = destination.getOtherEntry(curveEntry);
            }

            return (leftCurveEntry, rightCurveEntry);
        }
        
        private RoomPartRandom getRoomPart(MLDEntryType type,MapLayoutDataEntry start)
        {
            RoomPartRandom roomPart;

            if (type == MLDEntryType.CombatRoom || type == MLDEntryType.BossRoom)
            {
                if (start.beforeNode != null && start.beforeNode.isTurningPoint)
                {
                    roomPart = mapPartSet.getRandomRoomWithoutOffset(type,baseEntry.usedCombatRoom);
                }
                else
                {
                    roomPart = mapPartSet.getRandomRoom(type,baseEntry.usedCombatRoom);
                }
                
                baseEntry.usedCombatRoom.Add(roomPart.id);
            }
            else
            {
                roomPart = mapPartSet.getRandomRoom(type);
            }
            
            roomBuilder.setData(roomPart);

            return roomPart;
        }
        
        private bool tryChangeRoadPart(out RoadPartRandom beforeRoadPart)
        {
            bool isChanged =false;
            beforeRoadPart = null;
            
            if (baseEntry.installedRoadLength >= option.getRoadChangeLength())
            {
                isChanged = true;
                beforeRoadPart = baseEntry.cRoadPart;
                baseEntry.installedRoadLength = 0;
                baseEntry.cRoadPart = mapPartSet.getRandomRoad(baseEntry.cRoadPart.id);
            }
            
            baseEntry.startBridge = isChanged;
            roadBuilder.setData(baseEntry.cRoadPart);

            return isChanged;
        }
        
        private bool getPocketCount(MapLayoutDataEntry destination)
        {
            if (destination.type != MLDEntryType.RestRoom && destination.type != MLDEntryType.EndPoint)
            {
                PocketCount++;

                if (PocketCount >= option.pocketCount)
                {
                    PocketCount = 0;
                    return true;
                }
            }

            return false;
        }
        
        private MapLayoutDataEntry findCurve(MapLayoutDataEntry start)
        {
            Dir dir;

            while (true)
            {
                dir = start.getDir();
                if (start.isTurningPoint || isRoom(start.type))
                {
                    return start;
                }

                MapLayoutDataEntry next = start.next0;
                
                if (next == null)
                {
                    return start;
                }

                if (!dir.Equals(next.getDir()))
                {
                    return start;
                }

                start = next;
            }
        }

        private MapLayoutDataEntry getDestination(MapLayoutDataEntry start,out int count)
        {
            count = 0;
            while (true)
            {
                if (start.isTurningPoint)
                {
                    return start;
                }

                if (start.type == MLDEntryType.CombatRoom || start.type == MLDEntryType.RestRoom ||start.type == MLDEntryType.BossRoom||
                    start.type == MLDEntryType.EndPoint || start.type == MLDEntryType.SecretRoom)
                {
                    return start;
                }

                if (start.next0 == null)
                {
                    return start;
                }
                
                start = start.next0;
                count++;
            }
        }

        #region Push

        private void pushRoom(Vector2Int pos, MapLayoutDataEntry entry)
        {
            if (tryPush(pos, entry,out BuildEntry buildEntry))
            {
                buildEntry.startAtRoom = true;
            }
        }
        
        private void pushSecret(Vector2Int pos, MapLayoutDataEntry entry)
        {
            if (tryPush(pos, entry,out BuildEntry buildEntry))
            {
                buildEntry.fixedRoad = true;
                if (entry.type == MLDEntryType.SecretRoom)
                {
                    buildEntry.setRoomDistance(baseEntry.cRoadPart.secretRoad.roomMinDistance);
                }
                else
                {
                    buildEntry.makeSkillUpgrade = true;
                }
            }
        }
        
        private void push(Vector2Int pos,MapLayoutDataEntry entry)
        {
            if (entry != null)
            {
                BuildEntry buildEntry = new BuildEntry(baseEntry,pos, entry);
                stack.Push(buildEntry);
            }
        }

        private bool tryPush(Vector2Int pos,MapLayoutDataEntry entry,out BuildEntry result)
        {
            if (entry != null)
            {
                result = new BuildEntry(baseEntry,pos, entry);
                stack.Push(result);
                return true;
            }

            result = null;
            return false;
        }

        #endregion

        #region Utility

        private void setPos((Vector2Int, Vector2Int) poss)
        {
            cPos = poss.Item1;
            forkPos = poss.Item2;
        }
        
        private bool isRoom(MLDEntryType type)
        {
            return type != MLDEntryType.SecretRoad && type != MLDEntryType.Road;
        }
        
        public Vector2Int getPos(Vector2Int index)
        {
            return index * getCellSize();
        }
        
        private int getCellSize()
        {
            return option.cellHalfRange * 2 + 1;
        }
        
        #endregion
       
    }

    [Serializable]
    public class MapBuilderOption
    {
        public int cellHalfRange = 12;
        public int roadChangeCount = 2;
        public int pocketCount = 3;

        public int getRoadChangeLength()
        {
            return cellHalfRange * 2 * roadChangeCount;
        }
    }

    public class BuildEntry
    {
        public MapLayoutDataEntry data;
        public RoadPartRandom cRoadPart;
        public int installedRoadLength;
        public int roomDistance;
        public bool fixedRoad;
        public bool roomDistanceMode;
        public bool startAtRoom;
        public bool makeSkillUpgrade;
        public bool startBridge;
 
        public Vector2Int start;
        public HashSet<int> usedCombatRoom = new();

        
        public bool consumeStartBridge()
        {
            bool result = startBridge || startAtRoom;
            startBridge = false;
            startAtRoom = false;
            return result ;
        }

        public void setRoomDistance(int distance)
        {
            roomDistance = distance;
            roomDistanceMode = true;
        }
        public BuildEntry(Vector2Int start,MapLayoutDataEntry data)
        {
            if (data != null)
            {
                this.data = data;
            }

            this.start = start;
        }
        
        public BuildEntry(BuildEntry other,Vector2Int start,MapLayoutDataEntry data)
        {
            if (data != null)
            {
                this.data = data;
            }

            if (other != null)
            {
                installedRoadLength = other.installedRoadLength;
                cRoadPart = other.cRoadPart;
                usedCombatRoom.UnionWith(other.usedCombatRoom);
            }

            this.start = start;
        }
    }
}
