using System.Collections.Generic;
using UnityEngine;

namespace MapBuild
{
    /// <summary>
    /// 여러 파츠(도로/방/브릿지/시크릿 등)에서 생성되는 BlockMap들을
    /// 월드 좌표(Vector3Int) 기준으로 누적/덮어쓰기 하며 관리하는 조립 버퍼.
    ///
    /// - addBlockMap(): 파츠 BlockMap을 deepCopy + 회전 적용 후, pos 오프셋으로 blocksManager에 합성한다.
    ///   (동일 좌표는 "마지막에 들어온 BlockInfo"가 덮어쓴다.)
    ///
    /// - makeSkillUpgrade(): 특정 좌표의 블록 정보를 직접 수정해서( yAxisBlockList ) 보상 오브젝트를 심는다.
    ///   (전제: 해당 좌표에 BlockInfo가 이미 존재해야 함)
    ///
    /// - findMapSize(): 현재 누적된 블록들의 AABB(min/max)를 계산해 맵 크기와 bottomLeft를 구한다.
    /// - makingBlockMap(): bottomLeft 기준으로 좌표를 0부터 시작하도록 재정렬하여 1D 배열(BlockMap.oneDMap)에 패킹한다.
    ///   또한 startPos를 bottomLeft 기준 좌표로 변환하여 BlockMap.startPoint에 기록한다.
    ///
    /// 즉, 이 클래스는 "파츠 배치 → 누적 합성 → 최종 BlockMap 생성"만 담당하며,
    /// 경로/분기/방 선택 같은 상위 생성 규칙은 포함하지 않는다.
    /// </summary>
    public class BlockMapManager
    {
        private Dictionary<Vector3Int, BlockInfo> blocksManager = new();
        public Vector3Int startPos;

        public void initialize()
        {
            blocksManager.Clear();
            startPos = Vector3Int.zero;
        }

        public void makeSkillUpgrade(Vector2Int pos)
        {
            if(blocksManager.TryGetValue(new Vector3Int(pos.x,0,pos.y),out BlockInfo blockInfo))
            {
                YAxisBlock yAxisBlock = new YAxisBlock(152, Quaternion.identity);
                if (blockInfo.yAxisBlockList.Count > 0)
                {
                    blockInfo.yAxisBlockList[0] = yAxisBlock;
                }
                else
                {
                    blockInfo.yAxisBlockList.Add(yAxisBlock);
                }
            }
        }

        public void addBlockMap(BlockMap blockMap, Vector2Int pos, Quaternion quaternion)
        {
            Vector3Int newPos = new Vector3Int(pos.x, 0, pos.y);
            addBlockMap(blockMap, newPos, quaternion);
        }

        public void addBlockMap(BlockMap blockMap, Vector3Int pos, Quaternion quaternion)
        {
            BlockMap copy = blockMap.deepCopy();
            copy.rotate(quaternion);
            Dictionary<Vector3Int, BlockInfo> addingData = loadBlockMap(copy);

            foreach (var keyValuePair in addingData)
            {
                blocksManager[keyValuePair.Key + pos] = keyValuePair.Value;
            }
        }

        public BlockMap makingBlockMap(string mapName, Vector3Int mapSize, Vector3Int bottomLeft)
        {
            int sizeX = mapSize.x;
            int sizeZ = mapSize.z;

            BlockMap blockMap = new BlockMap(mapName, sizeX, sizeZ);

            blockMap.startPoint = startPos-bottomLeft;

            foreach (var keyValuePair in blocksManager)
            {
                if (keyValuePair.Value != null)
                {
                    Vector3Int
                        position = keyValuePair.Key -
                                   bottomLeft; //to adjust cIndex,the branchValue of min key is changed to (0,0,0)
                    int x = position.x;
                    int z = position.z;

                    blockMap.oneDMap[x * sizeZ + z] = keyValuePair.Value;
                }
            }

            return blockMap;
        }

        private Dictionary<Vector3Int, BlockInfo> loadBlockMap(BlockMap blockMap)
        {
            Dictionary<Vector3Int, BlockInfo> result = new();
            int count = 0;
            foreach (var blockInfo in blockMap.oneDMap)
            {
                if (blockInfo != null && blockInfo.id != -1)
                {
                    result.Add(blockMap.convertPositionFromIndex(count), blockInfo);
                }

                count++;
            }

            return result;
        }

        public Vector3Int findMapSize(out Vector3Int bottomLeft)
        {
            int maxX = 0, maxZ = 0, minX = 0, minZ = 0;
            bool isFirst = true;

            foreach (var keyValuePair in blocksManager)
            {
                int x = keyValuePair.Key.x;
                int z = keyValuePair.Key.z;

                if (isFirst) //to input default branchValue 
                {
                    maxX = x;
                    minX = x;
                    maxZ = z;
                    minZ = z;

                    isFirst = false;
                }

                if (x > maxX) maxX = x;
                if (x < minX) minX = x;
                if (z > maxZ) maxZ = z;
                if (z < minZ) minZ = z;
            }

            bottomLeft = new Vector3Int(minX, 0, minZ);

            return new Vector3Int(maxX - minX + 1, 0, maxZ - minZ + 1);
        }
    }
}