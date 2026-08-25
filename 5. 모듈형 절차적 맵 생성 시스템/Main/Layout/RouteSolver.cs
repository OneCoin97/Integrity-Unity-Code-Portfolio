using System.Collections.Generic;
using UnityEngine;

namespace MapBuild
 {
     /// <summary>
     /// MapLayoutGenerator의 현재 상태(cData, 옵션, RNG)를 기반으로,
     /// 특정 WaitData(시작 지점/방향/깊이)에서 다음 방까지 이어지는 "경로 후보(RoutePrinterData 체인)"를 탐색해 생성하는 클래스.
     /// 
     /// - 경로는 RouteMask(길 마스크) 조합으로 구성되며, 길/방 설치 가능 여부를 검사한다.
     /// - usedPosSet을 이용해 이번 탐색 시도 내에서 중복/충돌을 방지하고, 백트래킹 시 롤백한다.
     /// - 옵션/선물(Gift) 상태를 바탕으로 depth별 시크릿룸 설치 계획(installSecretDepthSet)을 수립하고,
     ///   경로 중 특정 depth에서 시크릿룸 설치 위치를 추가로 탐색한다.
     /// - 성공 시 시작부터 종료까지의 RoutePrinterData 리스트(routePrinters)를 반환한다.
     /// </summary>
     public class RouteSolver
     {
         private MapLayoutGenerator owner;
         private Vector2Int[] allDirs;

         private MapLayoutStageOption cStageOption
         {
             get { return owner.cStageOption; }
         }

         private MapLayoutOption layoutOption
         {
             get { return owner.layoutOption; }
         }

         private RandomUtility randomUtility
         {
             get { return owner.randomUtility; }
         }

         private MapLayoutData cData
         {
             get { return owner.cData; }
         }

         public RouteSolver(MapLayoutGenerator owner, Vector2Int[] allDirs)
         {
             this.owner = owner;
             this.allDirs = allDirs;
         }

         private HashSet<Vector2Int> usedPosSet = new();
         private HashSet<int> installSecretDepthSet = new();
         private Stack<RoutePrinterData> waitDatas = new();
         private Dictionary<int, RouteMask> maskDict = new();
         private int maxDepth;

         private HashSet<Vector2Int> absoluteMaskSet = new();

         private void initialize()
         {
             maskDict.Clear();
             usedPosSet.Clear();
             waitDatas.Clear();
             installSecretDepthSet.Clear();
             maxDepth = randomUtility.getRandomValue(cStageOption.routeRoomCountsMin, cStageOption.routeRoomCountsMax);
         }

         public bool tryBuildRoutePrinters(WaitData waitData, out List<RoutePrinterData> routePrinters)
         {
             initialize();
             routePrinters = new();

             if (waitData == null || maxDepth <= waitData.depth)
             {
                 return false;
             }

             if (!trySetupStart(waitData))
             {
                 return false;
             }

             RoutePrinterData before = null;

             while (waitDatas.Count > 0)
             {
                 RoutePrinterData routePrinterData = waitDatas.Pop();

                 rollbackToDepth(before, routePrinterData.depth);

                 if (!tryResolveSecretRoom(routePrinterData))
                 {
                     continue;
                 }

                 if (!tryApplyRoutePrinter(routePrinterData))
                 {
                     continue;
                 }

                 before = routePrinterData;

                 if (isRouteComplete(routePrinterData, out RoutePrinterData next))
                 {
                     buildRouteResult(next, routePrinters);
                     return true;
                 }
                 else
                 {
                     pushNextCandidates(next, routePrinterData, layoutOption.minRoomToRoomRoadLength,
                         layoutOption.maxRoomToRoomRoadLength);
                 }
             }

             return false;
         }

         private bool trySetupStart(WaitData waitData)
         {
             selectSecretRoomDepthSet(waitData);
             int startRange = Mathf.Min(layoutOption.maxRoomToRoomRoadLength - waitData.roadCount + 1,
                 layoutOption.maxRoomToRoomRoadLength);
             int startDepth = waitData.depth;

             if (startRange <= 0)
             {
                 return false;
             }

             RoutePrinterData start = new RoutePrinterData(waitData.entry.pos, waitData.dir);
             start.depth = startDepth;

             pushNextCandidates(start, null, startRange, startRange);

             foreach (var startInput in waitDatas)
             {
                 startInput.depth = startDepth;
             }

             return waitDatas.Count > 0;
         }

         private void rollbackToDepth(RoutePrinterData before, int targetDepth)
         {
             // before 체인 롤백 + usedPosSet 롤백
             while (before != null && before.depth >= targetDepth)
             {
                 Vector2Int pos = before.pos;
                 for (int i = 0; i < before.mask.Count; i++)
                 {
                     usedPosSet.Remove(before.mask[i] + pos);
                 }

                 if (before.installSecretRoom)
                 {
                     usedPosSet.Remove(before.secretPos);
                 }

                 before = before.before;
             }
         }


         private bool tryResolveSecretRoom(RoutePrinterData routePrinterData)
         {
             if (!routePrinterData.installSecretRoom)
             {
                 return true;
             }

             Vector2Int startPos = routePrinterData.pos;
             List<Vector2Int> mask = routePrinterData.mask;

             if (canInstallSecret(routePrinterData.depth == maxDepth, startPos, mask, out var result))
             {
                 routePrinterData.secretPos = result.Item1;
                 routePrinterData.secretBeforePos = result.Item2;
                 return true;
             }

             return false;
         }

         private bool tryApplyRoutePrinter(RoutePrinterData routePrinterData)
         {
             Vector2Int originPos = routePrinterData.pos;
             List<Vector2Int> maskOffsets = routePrinterData.mask;

             if (maskOffsets == null || maskOffsets.Count < 2)
             {
                 return false;
             }

             for (int i = 0; i < maskOffsets.Count - 1; i++)
             {
                 Vector2Int roadPos = originPos + maskOffsets[i];

                 if (cData.getEntry(roadPos) != null)
                 {
                     return false;
                 }

                 if (usedPosSet.Contains(roadPos))
                 {
                     return false;
                 }
             }

             Vector2Int destinationPos = originPos + maskOffsets[maskOffsets.Count - 1];

             if (!canInstallRoom(destinationPos))
             {
                 return false;
             }

             for (int i = 0; i < maskOffsets.Count; i++)
             {
                 Vector2Int roadPos = originPos + maskOffsets[i];
                 usedPosSet.Add(roadPos);
             }

             if (routePrinterData.installSecretRoom)
             {
                 usedPosSet.Add(routePrinterData.secretPos);
             }

             return true;
         }

         private bool isRouteComplete(RoutePrinterData routePrinterData, out RoutePrinterData next)
         {
             List<Vector2Int> mask = routePrinterData.mask;
             Vector2Int startPos = routePrinterData.pos;

             int count = mask.Count;

             // 전제: RouteMask는 항상 mask.Count >= 2인 마스크만 생성한다.
             // (length=1이어도 (1,0) road + (2,0) destination 형태로 최소 2개가 들어감)
             // 따라서 mask[count-2] 접근이 안전하다고 가정한다.
             Vector2Int destination = mask[count - 1];
             Vector2Int dir = destination - mask[count - 2];

             next = new RoutePrinterData(destination + startPos, dir);
             next.setBefore(routePrinterData);

             return next.depth > maxDepth;
         }

         private void buildRouteResult(RoutePrinterData next, List<RoutePrinterData> routePrinters)
         {
             next = next.before; // mask없는 더미데이터 제외

             Stack<RoutePrinterData> tempStack = new Stack<RoutePrinterData>();
             while (next != null)
             {
                 tempStack.Push(next);
                 next = next.before;
             }

             while (tempStack.Count > 0)
             {
                 routePrinters.Add(tempStack.Pop());
             }
         }

         private void selectSecretRoomDepthSet(WaitData waitData)
         {
             bool secretStart = waitData.isSecret;
             int startGift = waitData.startGift;
             int startDepth = waitData.depth;
             int endDepth = maxDepth;

             if (!layoutOption.canMakeSecretRoomOnLastDepth)
             {
                 endDepth--;
             }

             if (!layoutOption.canMakeSecretRoomAtSecretRoad && secretStart)
             {
                 startDepth++;
             }

             if (startDepth > endDepth)
             {
                 return;
             }

             int restDepth = endDepth - startDepth + 1;
             int restGift = randomUtility.getRandomValue(cStageOption.minGift, cStageOption.maxGift) - startGift;

             if (restGift <= 0)
             {
                 return;
             }

             if (restGift >= restDepth)
             {
                 for (int i = startDepth; i <= endDepth; i++)
                 {
                     installSecretDepthSet.Add(i);
                 }

                 return;
             }

             // 후보 depth 리스트 만들기
             List<int> depthList = new List<int>(restDepth);
             for (int i = startDepth; i <= endDepth; i++)
             {
                 depthList.Add(i);
             }

             randomUtility.shuffle(depthList);

             // 앞에서 restGift개 선택
             for (int i = 0; i < restGift; i++)
             {
                 installSecretDepthSet.Add(depthList[i]);
             }
         }

         private void pushNextCandidates(RoutePrinterData next, RoutePrinterData before, int min, int max)
         {
             bool secretRoom = installSecretDepthSet.Contains(next.depth);
             int range = randomUtility.getRandomValue(min, max);
             List<List<Vector2Int>> routeMaskList = getMask(range, next.dir);

             if (routeMaskList == null || routeMaskList.Count == 0)
             {
                 return;
             }

             randomUtility.shuffle(routeMaskList);

             foreach (var mask in routeMaskList)
             {
                 RoutePrinterData newNext = new RoutePrinterData(next, mask);
                 newNext.installSecretRoom = secretRoom;
                 newNext.setBefore(before);
                 waitDatas.Push(newNext);
             }
         }


         #region utility

         private bool canInstallRoom(Vector2Int destinationPos)
         {
             for (int x = -1; x <= 1; x++)
             {
                 for (int y = -1; y <= 1; y++)
                 {
                     Vector2Int checkPos = destinationPos + new Vector2Int(x, y);

                     if (cData.getEntry(checkPos) != null)
                     {
                         return false;
                     }

                     if (usedPosSet.Contains(checkPos))
                     {
                         return false;
                     }
                 }
             }

             return true;
         }

         private bool canInstallSecret(bool excludeLast, Vector2Int originPos, List<Vector2Int> mask,
             out (Vector2Int, Vector2Int) result)
         {
             if (mask == null || mask.Count == 0 || allDirs == null || allDirs.Length == 0)
             {
                 result = (new Vector2Int(), new Vector2Int());
                 return false;
             }

             // 1) mask 순회 순서 랜덤화 (오프셋 기준)
             List<Vector2Int> shuffledMask = new List<Vector2Int>(mask);
             if (excludeLast && shuffledMask.Count > 0)
             {
                 shuffledMask.RemoveAt(shuffledMask.Count - 1);
             }

             randomUtility.shuffle(shuffledMask);

             // 2) mask 포함 여부 체크용: 절대좌표 HashSet
             absoluteMaskSet.Clear();
             for (int i = 0; i < mask.Count; i++)
             {
                 absoluteMaskSet.Add(originPos + mask[i]);
             }

             // 3) 랜덤 순서로 마스크를 순회하며 설치 가능한 지점 탐색
             for (int i = 0; i < shuffledMask.Count; i++)
             {
                 Vector2Int startPoint = originPos + shuffledMask[i];

                 for (int d = 0; d < allDirs.Length; d++)
                 {
                     Vector2Int dir = allDirs[d];
                     Vector2Int installPos = startPoint + dir;

                     // 마스크 내부면 안 됨 + 이미 사용한 곳이면 안 됨
                     if (absoluteMaskSet.Contains(installPos) || usedPosSet.Contains(installPos))
                     {
                         continue;
                     }

                     if (canInstallRoom(installPos))
                     {
                         result = (installPos, startPoint);
                         return true;
                     }
                 }
             }

             result = (new Vector2Int(), new Vector2Int());
             return false;
         }

         private List<List<Vector2Int>> getMask(int length, Vector2Int dir)
         {
             // 전제: length는 1 이상이어야 실제 마스크가 생성된다.
             // length <= 0이면 RouteMask 내부 마스크가 비어있어 routeMaskList.Count == 0으로 실패 처리된다.
             length = Mathf.Max(length, 1);

             if (!maskDict.TryGetValue(length, out RouteMask routeMask))
             {
                 routeMask = new RouteMask(length);
                 maskDict[length] = routeMask;
             }

             return routeMask.getMask(dir);
         }

         #endregion
     }

     public class RoutePrinterData
     {
         public Vector2Int pos;
         public Vector2Int dir;
         public List<Vector2Int> mask = new();
         public RoutePrinterData before;
         public int depth = 0;

         public bool installSecretRoom;
         public Vector2Int secretBeforePos;
         public Vector2Int secretPos;

         public void setBefore(RoutePrinterData before)
         {
             if (before != null)
             {
                 this.before = before;
                 this.depth = before.depth + 1;
             }
         }

         public RoutePrinterData(Vector2Int pos, Vector2Int dir)
         {
             this.pos = pos;
             this.dir = dir;
         }

         public RoutePrinterData(RoutePrinterData original, List<Vector2Int> mask)
         {
             this.pos = original.pos;
             this.dir = original.dir;
             if (mask != null)
             {
                 this.mask = mask;
             }
         }
     }
 }
