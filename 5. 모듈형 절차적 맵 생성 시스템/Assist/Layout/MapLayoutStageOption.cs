using System;
using UnityEngine;

namespace MapBuild
{
    [CreateAssetMenu(menuName = "Map/MapLayoutStageOption")]
    public class MapLayoutStageOption : ScriptableObject
    {
        public int combatRoomCount = 20;

        public int seed;
        public bool useRandomSeed = true;

        public int minGift = 2;
        public int maxGift = 3;

        [Range(0, 1)]
        public float
            pRecoverBeforeLastCombat = 0.75f,
            pRecoverInDepthRange = 0.5f,
            pBossRoom = 1;

        public int minRecoverDepth = 2;
        public int maxRecoverDepth = 3;

        public int routeRoomCountsMax = 6;
        public int routeRoomCountsMin = 4;

        public int getSeedFromOption()
        {
            return useRandomSeed ? Environment.TickCount : seed;
        }
    }
}
