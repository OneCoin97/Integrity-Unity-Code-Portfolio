using System.Collections.Generic;
using UnityEngine;


namespace MapBuild
{
    /// <summary>
    /// RoomPartRandom(방 파츠) 데이터를 기반으로
    /// 실제 블록 단위의 방 구조를 맵에 설치하는 저수준 빌더.
    ///
    /// - 일반 방 설치(buildRoom / buildOnlyRoom)
    /// - 출구가 2개 이상인 방 설치(buildRoom(dir, forkDir) / buildRoomNoForward)
    /// - 전투방 + 시크릿 출구 설치(buildCombatRoomWithSecret)
    ///
    /// MapBuilder가 레이아웃 그래프(노드 연결, 다음 목적지 등)를 판단하고,
    /// RoomBuilder는 "지정 위치에 해당 방 맵을 회전/오프셋 적용해서 배치 + 병합"만 담당한다.
    ///
    /// 즉, 이 클래스는 "방 배치 전용 조립기"이며
    /// 분기 판단/경로 선택/생성 규칙 같은 상위 로직은 포함하지 않는다.
    /// </summary>
    public class RoomBuilder : MonoBehaviour
    {
        private BlockMapManager blockMapManager;
        private RoomPartRandom roomPart;
        private Map cMap;

        public void initialize(BlockMapManager blockMapManager, Map map)
        {
            this.blockMapManager = blockMapManager;
            this.cMap = map;
        }

        public void setData(RoomPartRandom roomPart)
        {
            this.roomPart = roomPart;
        }

        public Vector2Int buildRoom(Vector2Int start, Dir dir)
        {
            installRoom(start,dir,roomPart.main.map);

            return start + DirUtility.getOffset(dir, Vector2Int.zero,roomPart.main.getSizeX(), roomPart.main.getSizeZ()) + roomPart.main.getExit(dir) + DirUtility.getDirVector(dir);
        }
        
        public (Vector2Int, Vector2Int) buildCombatRoomWithSecret(Vector2Int start, Dir dir, Dir forkDir)
        {
            RoomSecretExit secretExit = roomPart.getSecretExit(dir, forkDir);
            if (secretExit != null)
            {
                Vector2Int offset = secretExit.offset;
                Vector2Int installPos = start + (roomPart.main.getSizeX()/2+offset.x) * DirUtility.getDirVector(dir)+ (roomPart.main.getSizeZ()/2+1+offset.y) * DirUtility.getDirVector(forkDir);
                installRoom(installPos,forkDir,secretExit.map);
            }
            
            Vector2Int next = buildRoom(start,dir);

            return (next, start + DirUtility.getOffset(dir, Vector2Int.zero,roomPart.main.getSizeX(), roomPart.main.getSizeZ()) + roomPart.main.getExit(dir,forkDir)+DirUtility.getDirVector(forkDir));
        }

        public void buildOnlyRoom(Vector2Int start, Dir dir)
        {
            installRoom(start,dir,roomPart.main.map);
        }

        public (Vector2Int, Vector2Int) buildRoom(Vector2Int start, Dir dir, Dir forkDir)
        {
            installRoom(start,dir,roomPart.main.map);

            start += DirUtility.getOffset(dir, Vector2Int.zero, roomPart.main.getSizeX(), roomPart.main.getSizeZ());

            return (start + roomPart.main.getExit(dir) + DirUtility.getDirVector(dir),start+ roomPart.main.getExit(dir,forkDir) + DirUtility.getDirVector(forkDir));
        }
        
        public (Vector2Int, Vector2Int) buildRoomNoForward(Vector2Int start, Dir dir)
        {
            installRoom(start,dir,roomPart.main.map);

            Dir left = DirUtility.getLeftDir(dir);
            Dir right = DirUtility.getReverseDir(left);

            start += DirUtility.getOffset(dir, Vector2Int.zero, roomPart.main.getSizeX(), roomPart.main.getSizeZ());

            return (start+ roomPart.main.getExit(dir,left) + DirUtility.getDirVector(left),start+ roomPart.main.getExit(dir,right) + DirUtility.getDirVector(right));
        }

        private void installRoom(Vector2Int start, Dir dir,Map otherMap)
        {
            Vector2Int temp = start + DirUtility.getOffset(dir, Vector2Int.zero, otherMap.blockMap.sizeX,otherMap.blockMap.sizeZ);
            Vector3Int position;
            Quaternion quaternion = DirUtility.getDirRotation(dir);

     
            position = new Vector3Int(temp.x, 0, temp.y);
            
            blockMapManager.addBlockMap(otherMap.blockMap,position,quaternion);
            cMap.merge(otherMap, position, quaternion);
        }
        
    }

}

