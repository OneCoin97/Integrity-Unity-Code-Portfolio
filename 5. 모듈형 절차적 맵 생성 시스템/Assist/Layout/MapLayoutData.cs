using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapBuild
{
    public class MapLayoutData
    {
        public int seed;
        public int width;
        public int height;
        public MapLayoutDataEntry startPoint;
        public MapLayoutDataEntry[,] data;

        // 월드좌표(x,y)를 data 인덱스로 바꿔주는 오프셋
        private int offsetX;
        private int offsetY;

        private const int expandStep = 100;

        public MapLayoutSortInfo getMapInfoCounter()
        {
            MapLayoutSortInfo mapLayoutSortInfo = new MapLayoutSortInfo();

            foreach (var entry in data)
            {
                if (entry != null)
                {
                    mapLayoutSortInfo.addCount(entry.type);
                }
            }

            return mapLayoutSortInfo;
        }

        public MapLayoutDataEntry addNewEntry(MLDEntryType type, Vector2Int pos, int routeIndex)
        {
            int x = pos.x;
            int y = pos.y;

            ensureCapacity(pos);

            int ix = x + offsetX;
            int iy = y + offsetY;

            if (data[ix, iy] != null)
            {
                return null;
            }

            MapLayoutDataEntry newEntry = new MapLayoutDataEntry(type, pos, routeIndex);
            data[ix, iy] = newEntry;

            return newEntry;
        }

        public MapLayoutDataEntry getEntry(Vector2Int pos)
        {
            int x = pos.x;
            int y = pos.y;

            int ix = x + offsetX;
            int iy = y + offsetY;

            // 범위 밖이면 확장하지 않고 null 반환
            if (ix < 0 || iy < 0 || ix >= width || iy >= height)
            {
                return null;
            }

            return data[ix, iy];
        }

        public void setEntry(Vector2Int pos, MapLayoutDataEntry entry)
        {
            int x = pos.x;
            int y = pos.y;

            ensureCapacity(pos);
            data[x + offsetX, y + offsetY] = entry;
        }

        public void ensureCapacity(Vector2Int pos)
        {
            int x = pos.x;
            int y = pos.y;

            int ix = x + offsetX;
            int iy = y + offsetY;

            int addLeft = 0;
            int addRight = 0;
            int addDown = 0;
            int addUp = 0;

            if (ix < 0)
            {
                addLeft = ((-ix - 1) / expandStep + 1) * expandStep;
            }
            else if (ix >= width)
            {
                addRight = ((ix - width) / expandStep + 1) * expandStep;
            }

            if (iy < 0)
            {
                addDown = ((-iy - 1) / expandStep + 1) * expandStep;
            }
            else if (iy >= height)
            {
                addUp = ((iy - height) / expandStep + 1) * expandStep;
            }

            if (addLeft == 0 && addRight == 0 && addDown == 0 && addUp == 0)
            {
                return;
            }

            int newWidth = width + addLeft + addRight;
            int newHeight = height + addDown + addUp;

            MapLayoutDataEntry[,] newData = new MapLayoutDataEntry[newWidth, newHeight];

            for (int ox = 0; ox < width; ox++)
            {
                for (int oy = 0; oy < height; oy++)
                {
                    newData[ox + addLeft, oy + addDown] = data[ox, oy];
                }
            }

            data = newData;
            width = newWidth;
            height = newHeight;

            offsetX += addLeft;
            offsetY += addDown;
        }

        public MapLayoutData(int seed)
        {
            this.seed = seed;
            width = expandStep;
            height = expandStep;
            data = new MapLayoutDataEntry[width, height];
            offsetX = width / 2;
            offsetY = height / 2;
        }
    }



    public class MapLayoutDataEntry
    {
        public MLDEntryType type;
        public Vector2Int pos;

        // 현재 노드로 들어오는 단일 이전 노드(부모). 
        // 전제: 본 레이아웃은 merge(여러 부모를 갖는 합류)를 만들지 않는다.
        public MapLayoutDataEntry beforeNode;

        // 다음으로 이어지는 연결 노드들(최대 3개). 
        // next0: 기본 진행 경로(직진/메인 연결), next1~2: 분기 연결(추가 갈래).
        public MapLayoutDataEntry next0;
        public MapLayoutDataEntry next1;
        public MapLayoutDataEntry next2;

        // 연결된 next 개수(0~3)
        public int nextCount;

        // Road/SecretRoad에서 다음 연결이 2개 이상이면 분기점(혹은 진행 갈림)으로 표시
        public bool isTurningPoint;

        public int routeMask;
        public int primaryRouteIndex;

        // 생성 단계에서의 깊이(방/길 진행 단계)
        public int depth;

        public bool lastCombatRoom;

        // 이 노드가 포함된 경로(루트) 인덱스들
        public HashSet<int> routeIndexSet;

        // 월드 배치 시 추가 오프셋(필요할 때만 사용)
        public Vector2Int offset;
        public bool isOffset;

        public MapLayoutDataEntry getOtherEntry(MapLayoutDataEntry other)
        {
            if (next0 != null && !next0.Equals(other))
            {
                return next0;
            }

            if (next1 != null && !next1.Equals(other))
            {
                return next1;

            }

            if (next2 != null && !next2.Equals(other))
            {
                return next2;
            }

            return null;
        }

        // 전제: 생성 로직이 beforeNode/pos 관계를 항상 일관되게 유지한다.
        // (next?.beforeNode == this가 항상 성립)
        public MapLayoutDataEntry getForwardEntry()
        {
            Dir dir = getDir();
            if (next0 != null && next0.getDir().Equals(dir))
            {
                return next0;
            }

            if (next1 != null && next1.getDir().Equals(dir))
            {
                return next1;
            }

            if (next2 != null && next2.getDir().Equals(dir))
            {
                return next2;
            }

            return null;
        }

        public MapLayoutDataEntry getCurveEntry()
        {
            Dir dir = getDir();


            if (next0 != null && !next0.getDir().Equals(dir))
            {
                return next0;
            }

            if (next1 != null && !next1.getDir().Equals(dir))
            {
                return next1;
            }

            if (next2 != null && !next2.getDir().Equals(dir))
            {
                return next2;
            }

            return null;
        }


        public Dir getDir()
        {
            if (beforeNode == null)
            {
                Debug.LogError("이전 노드가 설정되어있지 않음");
                return Dir.Right;
            }

            return DirUtility.getDirFromVector(pos - beforeNode.pos);
        }

        public void addOffset(Vector2Int offset)
        {
            this.offset = offset;
            isOffset = true;
        }

        public MapLayoutDataEntry(MLDEntryType type, Vector2Int pos, int primaryRouteIndex)
        {
            this.type = type;
            this.pos = pos;

            this.primaryRouteIndex = primaryRouteIndex;

            next0 = null;
            next1 = null;
            next2 = null;
            nextCount = 0;

            isTurningPoint = false;

            depth = 0;
            routeIndexSet = new HashSet<int>();
            if (primaryRouteIndex >= 0)
            {
                routeIndexSet.Add(primaryRouteIndex);
            }
        }

        public void move(Vector2Int bottomLeft)
        {
            pos -= bottomLeft;
        }

        public void addNext(MapLayoutDataEntry entry)
        {
            // 전제: 노드는 부모(beforeNode)를 하나만 가진다(merge 없음)
            if (entry == null)
            {
                Debug.LogError("null 데이터 입력됨");
                return;
            }

            if (entry.beforeNode != null)
            {
                Debug.LogError("이미 연결된 부모노드가 있음");
                return;
            }

            if (nextCount >= 3)
            {
                Debug.LogError("더이상 연결이 불가능함");
                return;
            }

            entry.beforeNode = this;

            if (nextCount == 0) next0 = entry;
            else if (nextCount == 1) next1 = entry;
            else
            {
                next2 = entry;
            }

            nextCount++;

            updateTurningPoint();
        }


        public void addRouteIndexToAllNext(int routeIndex)
        {
            if (routeIndex < 0)
            {
                return;
            }

            HashSet<MapLayoutDataEntry> visited = new HashSet<MapLayoutDataEntry>();
            Stack<MapLayoutDataEntry> stack = new Stack<MapLayoutDataEntry>();

            stack.Push(this);

            while (stack.Count > 0)
            {
                MapLayoutDataEntry node = stack.Pop();
                if (node == null)
                {
                    continue;
                }

                if (visited.Contains(node))
                {
                    continue;
                }

                visited.Add(node);

                node.routeIndexSet.Add(routeIndex);

                if (node.next0 != null) stack.Push(node.next0);
                if (node.next1 != null) stack.Push(node.next1);
                if (node.next2 != null) stack.Push(node.next2);
            }
        }


        private void updateTurningPoint()
        {
            // Road/SecretRoad가 아니면 변곡점 개념이 의미 없으니 끔
            if (type != MLDEntryType.Road && type != MLDEntryType.SecretRoad)
            {
                isTurningPoint = false;
                return;
            }

            int count = 0;

            if (next0 != null) count++;
            if (next1 != null) count++;
            if (next2 != null) count++;

            isTurningPoint = count >= 2;
        }
    }


    [Serializable]
    public class MapLayoutSortInfo : IEnumerable<MapLayoutSortInfo.MICEntry>
    {
        [SerializeField] private List<MICEntry> data = new();

        // 인스펙터에는 안 보이게, 런타임 캐시로만 사용
        [NonSerialized] private Dictionary<MLDEntryType, MICEntry> dataDict;

        public void addCount(MLDEntryType type)
        {
            ensureDict();

            if (!dataDict.TryGetValue(type, out MICEntry entry))
            {
                MICEntry newEntry = new MICEntry();
                newEntry.type = type;
                newEntry.count = 1;
                data.Add(newEntry);
                dataDict[type] = newEntry;

                return;
            }

            entry.count++;
        }

        public void removeCount(MLDEntryType type)
        {
            ensureDict();

            if (dataDict.TryGetValue(type, out MICEntry entry))
            {
                entry.count--;
            }
        }

        public int getCount(MLDEntryType type)
        {
            ensureDict();

            if (dataDict.TryGetValue(type, out MICEntry entry))
            {
                return entry.count;
            }

            return 0;
        }

        // 필요하면 외부에서 전체 초기화도 가능
        public void resetAll()
        {
            ensureDict();

            for (int i = 0; i < data.Count; i++)
            {
                if (data[i] == null)
                {
                    continue;
                }

                data[i].count = 0;
            }
        }

        private void ensureDict()
        {
            if (dataDict != null)
            {
                return;
            }

            dataDict = new Dictionary<MLDEntryType, MICEntry>();

            if (data == null)
            {
                data = new List<MICEntry>();
                return;
            }

            // 리스트 기반(인스펙터) 데이터를 딕셔너리로 재구성
            for (int i = 0; i < data.Count; i++)
            {
                MICEntry entry = data[i];
                if (entry == null)
                {
                    continue;
                }

                // 중복 type이 있으면 "처음 것"을 우선으로 두고, 중복은 무시(인스펙터 실수 방어)
                if (dataDict.ContainsKey(entry.type))
                {
                    continue;
                }

                dataDict.Add(entry.type, entry);
            }
        }

        // ===== 순회 지원 =====
        public IEnumerator<MICEntry> GetEnumerator()
        {
            if (data == null)
            {
                yield break;
            }

            for (int i = 0; i < data.Count; i++)
            {
                MICEntry entry = data[i];
                if (entry == null)
                {
                    continue;
                }

                yield return entry;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Serializable]
        public class MICEntry
        {
            //[ReadOnly]
            public MLDEntryType type;

            //[ReadOnly]
            public int count;
        }
    }
    
    public enum MLDEntryType
    {
        Empty,
        StartPoint,
        EndPoint,
        CombatRoom,
        RestRoom,
        SecretRoom,
        Road,
        SecretRoad,
        BossRoom
    }
}