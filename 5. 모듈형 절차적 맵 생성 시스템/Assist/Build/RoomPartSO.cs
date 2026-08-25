using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace MapBuild
{
    [CreateAssetMenu(fileName = "RoomPartSO", menuName = "Map/RoomPartSO", order = 1)]
    public class RoomPartSO : ScriptableObject
    {
        public RoomPart data;
    }

    public class RoomPartRandom
    {
        public int id;
        public RoomType type;
        public RoomMain main;
        public RoomSecretExit up;
        public RoomSecretExit down;

        public int getCenterLength()
        {
            return main.getSizeX() / 2;
        }

        public bool haveRoomOffset(out int offset)
        {
            offset = main.getSizeZ() / 2 - main.getEntrance().y;

            return offset != 0;
        }

        public RoomSecretExit getSecretExit(Dir dir,Dir forkDir)
        {
            if (DirUtility.isUpDir(dir, forkDir))
            {
                return up;
            }
            else
            {
                return down;
            }
        }
    }

    [Serializable]
    public class RoomPart : IDeepCopyable<RoomPart>
    {
        public int id;
        public RoomType type;
        public RoomMain main;
        
        [Header("SecretExit")] 
        public List<RoomSecretExitCapsule> up = new ();
        public List<RoomSecretExitCapsule> down = new ();
        
        public bool haveRoomOffset()
        {
            int offset = main.getSizeZ() / 2 - main.getEntrance().y;

            return offset != 0;
        }
        
        public RoomPartRandom getRandomData(Random rng)
        {
            RoomPartRandom result = new ();
            
            if (rng != null)
            {
                result.id = id;
                result.type = type;
                result.main = main;
                result.up = getRandomExit(rng, up);
                result.down = getRandomExit(rng, down);

                return result;
            }

            return null;
        }

        private RoomSecretExit getRandomExit(Random rng, List<RoomSecretExitCapsule> list)
        {
            RoomSecretExitCapsule exitCapsule = getRandom(rng, list);

            if (exitCapsule != null && exitCapsule.data != null)
            {
                RoomSecretExit result = exitCapsule.data.data.deepCopy();
                result.offset += exitCapsule.offset;
                return result;
            }

            return null;
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
        
        
        public RoomPart deepCopy()
        {
            RoomPart result = new RoomPart();

            result.id = id;
            result.type = type;
            result.main = main.deepCopy();
            
            foreach (var secretExit in up)
            {
                result.up.Add(secretExit);
            }
            
            foreach (var secretExit in down)
            {
                result.down.Add(secretExit);
            }

            return result;
        }
    }

    public struct Exit
    {
        public Dir dir;
        public Vector2Int exit;

        public Exit(Dir dir, Vector2Int exit)
        {
            this.exit = exit;
            this.dir = dir;
        }

    }

    [Serializable]
    public class RoomMain : MapPart<RoomMain>
    {
        [SerializeField]
        private Vector2Int entrance;  //Left

        [SerializeField] 
        private Vector2Int exitR,exitU,exitD;

        public Vector2Int centerOffset;

        public RoomCombatType getCombatType()
        {
            if (map.roomDatas.data.Count > 0)
            {
                return map.roomDatas.data[0].roomCombatType;
            }
            
            return RoomCombatType.Null;
        }

        public Vector2Int getEntrance()
        {
            return entrance;
        }
        
        public Vector2Int getEntrance(Dir dir)
        {
            return rotate(entrance, dir);
        }
        
        public Vector2Int getExit(Dir dir)
        {
            return rotate(exitR, dir);
        }

        public Vector2Int getExit(Dir dir,Dir exitDir)
        {
            if (exitDir.Equals(dir))
            {
                return rotate(exitR, dir);
            }

            if (DirUtility.getLeftDir(dir).Equals(exitDir))
            {
                return rotate(exitU, dir);
            }
            else
            {
                return rotate(exitD, dir);
            }
        }

        protected override void copyTo(RoomMain data)
        {
            data.entrance = entrance;
            data.exitR = exitR;
            data.exitU = exitU;
            data.exitD = exitD;
        }
    }


}