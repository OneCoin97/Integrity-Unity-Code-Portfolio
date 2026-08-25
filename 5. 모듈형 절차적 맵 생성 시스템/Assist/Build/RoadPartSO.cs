using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
{
    [CreateAssetMenu(fileName = "RoadPart", menuName = "Map/RoadPartSO", order = 1)]
    public class RoadPartSO : ScriptableObject
    {
        public RoadPart data = new RoadPart();
    }

    public class RoadPartRandom
    {
        public int id;
        public Main main;
        public Fork fork;
        public SecretRoad secretRoad;
        public PartDefault bridge;
        public PartDefault pathPocket;
        
    }

    [Serializable]
    public class RoadPart : IDeepCopyable<RoadPart>
    {
        public int id;
        public Main main;
        public List<Fork> forkList = new();
        public List<SecretRoad> secretRoadList = new();
        public List<PartDefault> bridgeList = new();
        public List<PartDefault> pathPocketList = new(); 
        
        
        public RoadPartRandom getRandomData(Random rng)
        {
            RoadPartRandom result = new RoadPartRandom();
            
            if (rng != null)
            {
                result.id = id;
                result.main = main;
                result.fork = getRandom(rng, forkList);
                result.secretRoad = getRandom(rng, secretRoadList);
                result.bridge = getRandom(rng, bridgeList);
                result.pathPocket = getRandom(rng, pathPocketList);
                

                return result;
            }

            return null;
        }

        public void updateSecretRoad(Random rng,RoadPartRandom roadPartRandom)
        {
            if (roadPartRandom != null)
            {
                roadPartRandom.secretRoad = getRandomExcept(rng, secretRoadList, roadPartRandom.secretRoad);
            }
        }
        
        private T getRandomExcept<T>(Random rng, List<T> list, T exceptEntry) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            // 선택지가 1개면 제외 로직 의미 없으니 그냥 그거 반환
            if (list.Count == 1)
            {
                return list[0];
            }

            // exceptEntry가 리스트에 없으면 일반 랜덤과 동일
            bool hasExcept = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], exceptEntry))
                {
                    hasExcept = true;
                    break;
                }
            }

            if (!hasExcept)
            {
                int index = rng.Next(0, list.Count);
                return list[index];
            }

            // exceptEntry를 제외한 후보 개수
            int candidateCount = list.Count - 1;

            // 후보가 0이면(이론상 list.Count==1일 때) 위에서 처리되지만, 안전장치로 유지
            if (candidateCount <= 0)
            {
                return list[0];
            }

            // 0 ~ candidateCount-1 중 하나를 뽑고, exceptEntry를 건너뛰며 매핑
            int pick = rng.Next(0, candidateCount);
            int current = 0;

            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (ReferenceEquals(item, exceptEntry))
                {
                    continue;
                }

                if (current == pick)
                {
                    return item;
                }

                current++;
            }

            // 여기로 오면 논리적으로 이상하지만, 방어적으로 처리
            return list[0];
        }

        
        
        private T getRandom<T>(Random rng, List<T> list) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            int index = rng.Next(0, list.Count);
            return list[index];
        }
        
      

        public RoadPart deepCopy()
        {
            RoadPart roadPart = new RoadPart();

            roadPart.id = id;
            if (main != null)
            {
                roadPart.main = main.deepCopy();
            }

            if (forkList != null)
            {
                roadPart.forkList = new List<Fork>(forkList.Count);
                for (int i = 0; i < forkList.Count; i++)
                {
                    Fork item = forkList[i];
                    roadPart.forkList.Add(item != null ? item.deepCopy() : null);
                }
            }

            if (bridgeList != null)
            {
                roadPart.bridgeList = new List<PartDefault>(bridgeList.Count);
                for (int i = 0; i < bridgeList.Count; i++)
                {
                    PartDefault item = bridgeList[i];
                    roadPart.bridgeList.Add(item != null ? item.deepCopy() : null);
                }
            }
            
            if (secretRoadList != null)
            {
                roadPart.secretRoadList = new List<SecretRoad>(secretRoadList.Count);
                for (int i = 0; i < secretRoadList.Count; i++)
                {
                    SecretRoad item = secretRoadList[i];
                    roadPart.secretRoadList.Add(item != null ? item.deepCopy() : null);
                }
            }

            if (pathPocketList != null)
            {
                roadPart.pathPocketList = new List<PartDefault>(pathPocketList.Count);
                for (int i = 0; i < pathPocketList.Count; i++)
                {
                    PartDefault item = pathPocketList[i];
                    roadPart.pathPocketList.Add(item != null ? item.deepCopy() : null);
                }
            }

            return roadPart;
        }
    }

    [Serializable]
    public class Main : MapPart<Main>
    {
        public RoadType type;
        public int repeatRange; // 길의 반복되는 기본 단위
        public int halfWidth; // 길폭의 절반값

        protected override void copyTo(Main data)
        {
            data.type = type;
            data.repeatRange = repeatRange;
            data.halfWidth = halfWidth;
        }
    }

    [Serializable]
    public class PartDefault : MapPart<PartDefault>
    {
        public int overWriteRange = 0;
        protected override void copyTo(PartDefault data)
        {
            data.overWriteRange = overWriteRange;
        }
        
    }

    [Serializable]
    public class Fork : MapPart<Fork>
    {
        public int inner; // 내부길
        public int overWriteRange = 0;
        public int startOverWriteRange = 0;

        protected override void copyTo(Fork data)
        {
            data.inner = inner;
            data.overWriteRange = overWriteRange;
            data.startOverWriteRange = startOverWriteRange;
        }
    }

    [Serializable]
    public class SecretRoad : MapPart<SecretRoad>
    {
        public Vector2Int forkPos;
        public int overWriteRange = 0;
        public int startOverWriteRange = 0;
        public int roomMinDistance = 0;

        public Vector2Int getForkPos(bool isReverse)
        {
            if (!isReverse ||map == null || map.blockMap == null)
            {
                return forkPos;
            }

            Vector2 center = map.blockMap.center;
            Vector2 rotatedPos = center * 2 - forkPos;

            return Vector2Int.RoundToInt(rotatedPos);
        }

        protected override void copyTo(SecretRoad data)
        {
            data.forkPos = forkPos;
            data.overWriteRange = overWriteRange;
            data.startOverWriteRange = startOverWriteRange;
            data.roomMinDistance = roomMinDistance;
        }
    }
          

    [Serializable]
    public abstract class MapPart<T> : IDeepCopyable<T> where T : MapPart<T>,new()
    {
        public TextAsset textMap;
        [NonSerialized] public Map map;
        public Vector2Int offset;
        

        // 전제: 회전은 90도 단위만 지원(Quaternion y를 90 단위로 반올림). 그 외 각도는 사실상 0도 취급으로 fallback.
        public Vector2Int rotate(Vector2Int pos, Dir dir)
        {
            return rotate(pos, DirUtility.getDirRotation(dir));
        }
        
        public Vector2Int rotate(Vector2Int pos,Quaternion quaternion)
        {
            int yAngle = Mathf.RoundToInt(quaternion.eulerAngles.y / 90f) * 90;
            int normalizedAngle = ((yAngle % 360) + 360) % 360;

            int sizeX = map.blockMap.sizeX;
            int sizeZ = map.blockMap.sizeZ;
            
            int x = pos.x;
            int z = pos.y;
            
            int dstX;
            int dstZ;

            switch (normalizedAngle)
            {
                case 0:
                    dstX = x;
                    dstZ = z;
                    break;

                case 90:
                    dstX = z;
                    dstZ = (sizeX - 1) - x;
                    break;

                case 180:
                    dstX = (sizeX - 1) - x;
                    dstZ = (sizeZ - 1) - z;
                    break;

                case 270:
                    dstX = (sizeZ - 1) - z;
                    dstZ = x;
                    break;

                default:
                    // 90도 단위가 아니면 여기로 올 수 있는데, 현재 로직상 거의 없음
                    dstX = x;
                    dstZ = z;
                    break;
            }

            return new Vector2Int(dstX, dstZ);
        }
        public virtual int getSizeX()
        {
            return map.blockMap.sizeX;
        }

        public virtual int getSizeZ()
        {
            return map.blockMap.sizeZ;
        }
       
        protected abstract void copyTo(T data);

        public Map getNewMap()
        {
            return JsonUtility.FromJson<Map>(textMap.text);
        }

        
        // 정책: deepCopy 시 map은 runtime 상태 복제가 아니라 textMap 기반으로 새로 생성(동일 원본에서 항상 재구성).
        public T deepCopy()
        {
            T result = new T();

            // TextAsset 참조 복사 (같은 클래스라 private 접근 가능)
            result.textMap = textMap;
            result.offset = offset;

            // Map 복사: 1) textMap 있으면 항상 거기서 새로 생성 (안정적)
            if (textMap != null)
            {
                result.map = JsonUtility.FromJson<Map>(textMap.text);
            }
            else
            {
                result.map = null;
            }

            copyTo(result);

            return result;
        }
    }

    public enum RoadType
    {
        Default,
        WithWall,
    }



}
