using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
{
    [CreateAssetMenu(fileName = "RoomSecretExit", menuName = "Map/RoomSecretExit", order = 1)]
    public class RoomSecretExitSO : ScriptableObject
    {
        public RoomSecretExit data;
    }

    [Serializable]
    public class RoomSecretExit : MapPart<RoomSecretExit>
    {
        public int roomDistance;
        public List<RoadPartSO> roadParts = new List<RoadPartSO>();

        protected override void copyTo(RoomSecretExit data)
        {
            data.roomDistance = roomDistance;

            foreach (var roadPartSo in roadParts)
            {
                data.roadParts.Add(roadPartSo);
            }
        }

        public RoadPartRandom getSecretRoadPartFromRoom(Random rng)
        {
            if (roadParts == null || roadParts.Count <= 0)
            {
                return null;
            }

            if (rng == null)
            {
                rng = new Random();
            }

            // null 요소가 섞여있을 수 있으니 최대 N번 시도
            int tryCount = roadParts.Count;

            for (int i = 0; i < tryCount; i++)
            {
                int index = rng.Next(0, roadParts.Count);
                RoadPartSO result = roadParts[index];

                if (result != null && result.data != null)
                {
                    return result.data.deepCopy().getRandomData(rng);
                }
            }

            // 전부 null이면 null 반환
            return null;
        }
    }

    [Serializable]
    public class RoomSecretExitCapsule
    {
        public RoomSecretExitSO data;
        public Vector2Int offset;
    }
}