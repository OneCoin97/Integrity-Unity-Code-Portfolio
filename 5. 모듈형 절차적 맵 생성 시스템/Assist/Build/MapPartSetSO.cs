using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
{
    [Serializable]
    public class RoomPartList
    {
        public MLDEntryType type;
        public List<RoomPartSO> data = new List<RoomPartSO>();

        public RoomPartList(MLDEntryType type)
        {
            this.type = type;
        }

        public List<RoomPart> getData()
        {
            List<RoomPart> result = new();
            for (int i = 0; i < data.Count; i++)
            {
                RoomPartSO itemSo = data[i];
                if (itemSo == null)
                {
                    continue;
                }

                result.Add(itemSo.data.deepCopy());
            }

            return result;
        }
    }

    [CreateAssetMenu(menuName = "Map/Map Part Set")]
    public class MapPartSetSO : ScriptableObject
    {
        [SerializeField] private RoadPartSO roadPartDefault = null;

        [SerializeField] private List<RoadPartSO> roadPartList = new();

        [SerializeField] private List<RoomPartList> roomParts = new()
        {
            new RoomPartList(MLDEntryType.CombatRoom),
            new RoomPartList(MLDEntryType.BossRoom),
            new RoomPartList(MLDEntryType.SecretRoom),
            new RoomPartList(MLDEntryType.RestRoom),
            new RoomPartList(MLDEntryType.StartPoint),
            new RoomPartList(MLDEntryType.EndPoint),
        };
        
        public MapPartSet getData(Random rng)
        {
            if (rng == null)
            {
                rng = new Random();
            }

            if (this.roadPartDefault == null || this.roadPartDefault.data == null)
            {
                Debug.LogError("MapPartSetSO.getData() failed: roadPartDefault is null (required).");
                return null;
            }

            RoadPart roadPartDefault = this.roadPartDefault.data.deepCopy();

            List<RoadPart> roadPartList = new();

            for (int i = 0; i < this.roadPartList.Count; i++)
            {
                RoadPartSO itemSo = this.roadPartList[i];

                if (itemSo == null)
                {
                    continue;
                }

                roadPartList.Add(itemSo.data.deepCopy());
            }

            // 최종 생성
            MapPartSet result = new MapPartSet(roadPartDefault, roadPartList, roomParts, rng);

            return result;
        }

    }

    public class MapPartSet
    {
        private readonly RoadPart roadPartDefault;
        private readonly List<RoadPart> roadParts;
        private readonly Dictionary<int, RoadPart> roadPartsById = new Dictionary<int, RoadPart>();
        private readonly Dictionary<MLDEntryType, List<RoomPart>> roomsDatas = new Dictionary<MLDEntryType, List<RoomPart>>();
        private readonly Random rng;

        public MapPartSet(RoadPart roadPartDefault, List<RoadPart> roadParts, List<RoomPartList> roomsDatas, Random rng)
        {
            this.roadPartDefault = roadPartDefault;
            this.roadParts = roadParts ?? new List<RoadPart>();
            this.rng = rng != null ? rng : new Random();

            for (int i = 0; i < this.roadParts.Count; i++)
            {
                RoadPart roadPart = this.roadParts[i];
                if (roadPart != null && !roadPartsById.ContainsKey(roadPart.id))
                {
                    roadPartsById.Add(roadPart.id, roadPart);
                }
            }

            foreach (var roomPartList in roomsDatas)
            {
                if (!this.roomsDatas.TryGetValue(roomPartList.type, out List<RoomPart> data))
                {
                    data = new List<RoomPart>();
                    this.roomsDatas[roomPartList.type] = data;
                }

                int id = 0;

                if (data.Count > 0)
                {
                    id = data[data.Count - 1].id + 1;
                }

                foreach (var roomPart in roomPartList.getData())
                {
                    roomPart.id = id++;
                    data.Add(roomPart);
                }
            }
        }


        public void updateSecretRoad(RoadPartRandom roadPartRandom)
        {
            if (roadPartRandom == null)
            {
                return;
            }

            if (roadPartsById.TryGetValue(roadPartRandom.id, out RoadPart roadPart))
            {
                roadPart.updateSecretRoad(rng, roadPartRandom);
            }
        }

        public RoadPartRandom getDefaultRoad()
        {
            return roadPartDefault.getRandomData(rng);
        }

        /// <summary>
        /// id 파라미터는 "제외할 road id" 로 사용.
        /// id == -1 이면 제외 없이 랜덤.
        /// </summary>
        public RoadPartRandom getRandomRoad(int id = -1)
        {
            if (roadParts == null || roadParts.Count <= 0)
            {
                return roadPartDefault.getRandomData(rng);
            }

            int count = roadParts.Count;

            for (int i = 0; i < count; i++)
            {
                RoadPart roadPart = roadParts[rng.Next(0, count)];

                if (roadPart.id != id)
                {
                    return roadPart.getRandomData(rng);
                }
            }

            return roadPartDefault.getRandomData(rng);
        }


        public RoomPartRandom getRandomRoom(MLDEntryType type)
        {
            if (!roomsDatas.TryGetValue(type, out List<RoomPart> data))
            {
                return null;
            }

            if (data == null || data.Count <= 0)
            {
                return null;
            }

            int pickIndex = rng.Next(0, data.Count);
            return data[pickIndex].getRandomData(rng);
        }

        public RoomPartRandom getRandomRoomWithoutOffset(MLDEntryType type, HashSet<int> excludeRoomIds)
        {
            if (!roomsDatas.TryGetValue(type, out List<RoomPart> data))
            {
                return null;
            }

            if (data == null || data.Count <= 0)
            {
                return null;
            }

            // 제외 조건이 없으면 기존 로직과 동일
            if (excludeRoomIds == null || excludeRoomIds.Count <= 0)
            {
                int pickIndex = rng.Next(0, data.Count);
                return data[pickIndex].getRandomData(rng);
            }

            // 후보 인덱스 수집
            List<int> candidateIndices = new List<int>(data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                RoomPart roomPart = data[i];
                if (roomPart == null)
                {
                    continue;
                }

                if (excludeRoomIds.Contains(roomPart.id))
                {
                    continue;
                }

                if (roomPart.haveRoomOffset())
                {
                    continue;
                }

                candidateIndices.Add(i);
            }


            if (candidateIndices.Count <= 0)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    RoomPart roomPart = data[i];
                    if (roomPart == null)
                    {
                        continue;
                    }

                    if (roomPart.haveRoomOffset())
                    {
                        continue;
                    }

                    candidateIndices.Add(i);
                }
            }

            if (candidateIndices.Count <= 0)
            {
                int pickIndex = rng.Next(0, data.Count);
                return data[pickIndex].getRandomData(rng);
            }

            int picked = rng.Next(0, candidateIndices.Count);
            int finalIndex = candidateIndices[picked];

            RoomPart selected = data[finalIndex];
            if (selected == null)
            {
                return null;
            }

            return selected.getRandomData(rng);
        }

        /// <summary>
        /// excludeRoomIds 에 들어있는 RoomPart.id 들은 전부 제외하고 랜덤 선택.
        /// 전부 제외되면 null 반환.
        /// </summary>
        public RoomPartRandom getRandomRoom(MLDEntryType type, HashSet<int> excludeRoomIds)
        {
            if (!roomsDatas.TryGetValue(type, out List<RoomPart> data))
            {
                return null;
            }

            if (data == null || data.Count <= 0)
            {
                return null;
            }

            // 제외 조건이 없으면 기존 로직과 동일
            if (excludeRoomIds == null || excludeRoomIds.Count <= 0)
            {
                int pickIndex = rng.Next(0, data.Count);
                return data[pickIndex].getRandomData(rng);
            }

            // 후보 인덱스 수집
            List<int> candidateIndices = new List<int>(data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                RoomPart roomPart = data[i];
                if (roomPart == null)
                {
                    continue;
                }

                if (excludeRoomIds.Contains(roomPart.id))
                {
                    continue;
                }

                candidateIndices.Add(i);
            }

            if (candidateIndices.Count <= 0)
            {
                int pickIndex = rng.Next(0, data.Count);
                return data[pickIndex].getRandomData(rng);
            }

            int picked = rng.Next(0, candidateIndices.Count);
            int finalIndex = candidateIndices[picked];

            RoomPart selected = data[finalIndex];
            if (selected == null)
            {
                return null;
            }

            return selected.getRandomData(rng);
        }
    }
}
