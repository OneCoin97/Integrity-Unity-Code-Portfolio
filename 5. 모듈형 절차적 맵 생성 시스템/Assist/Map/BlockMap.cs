using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockMap : IDeepCopyable<BlockMap>
{
    public string name; //맵 이름
    public Vector2 center;
    public Vector3 startPoint;
    public int sizeX; // 맵 사이즈 X
    public int sizeZ; //맵 사이즈 Z
    public BlockInfo[] oneDMap; //직렬화 하기위한 1차원 배열

    public BlockMap(string name, int sizeX, int sizeZ)
    {
        this.name = name;
        this.sizeX = sizeX;
        this.sizeZ = sizeZ;

        float centerX = (sizeX - 1) / 2f;
        float centerZ = (sizeZ - 1) / 2f;
        center = new Vector2(centerX, centerZ);

        oneDMap = new BlockInfo[this.sizeX * this.sizeZ];
    }

    public BlockMap deepCopy()
    {
        BlockMap result = new BlockMap(name, sizeX, sizeZ);

        int length = oneDMap.Length;
        
        for (int i = 0; i < length; i++)
        {
            BlockInfo info = oneDMap[i];
            if (info != null)
            {
                result.oneDMap[i] = info.deepCopy();
            }
            else
            {
                result.oneDMap[i] = null;
            }
        }
        
        return result;
    }

    public BlockMap createSubBlockMap(Vector2Int start, Vector2Int end)
    {
        // Vector2Int: (x, z)로 사용한다고 가정 (y를 z로 취급)
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minZ = Mathf.Min(start.y, end.y);
        int maxZ = Mathf.Max(start.y, end.y);

        // 원본 맵 범위로 클램프
        minX = Mathf.Clamp(minX, 0, sizeX - 1);
        maxX = Mathf.Clamp(maxX, 0, sizeX - 1);
        minZ = Mathf.Clamp(minZ, 0, sizeZ - 1);
        maxZ = Mathf.Clamp(maxZ, 0, sizeZ - 1);

        int newSizeX = maxX - minX + 1;
        int newSizeZ = maxZ - minZ + 1;

        if (newSizeX <= 0 || newSizeZ <= 0)
        {
            throw new ArgumentException($"Invalid range: start={start}, end={end}");
        }

        string newName = $"{name}_{minX}_{minZ}_{maxX}_{maxZ}";
        BlockMap result = new BlockMap(newName, newSizeX, newSizeZ);


        // 데이터 복사 (BlockInfo가 class면 참조 복사 / struct면 값 복사)
        for (int x = 0; x < newSizeX; x++)
        {
            for (int z = 0; z < newSizeZ; z++)
            {
                int srcX = minX + x;
                int srcZ = minZ + z;

                result[x, z] = this[srcX, srcZ];
            }
        }

        return result;
    }

    public Vector3Int convertPositionFromIndex(int index)
    {
        return new Vector3Int(index / sizeZ, 0, index % sizeZ);
    }

    ///인덱서를 재정의하여 1차원 맵을 2차원처럼 다룸
    public BlockInfo this[int x, int z]
    {
        get
        {
            int index = x * sizeZ + z;
            if (index < oneDMap.Length && index >= 0 && z < sizeZ && x < sizeX)
            {
                return oneDMap[index];
            }
            else
            {
                return null; // 인덱스가 범위를 벗어났을 때
            }
        }
        set
        {
            int index = x * sizeZ + z;
            if (index < oneDMap.Length && index >= 0 && z < sizeZ && x < sizeX)
            {
                oneDMap[index] = value;
            }
            else
            {
                // 인덱스가 범위를 벗어났을 때의 처리 (예외 처리 등)
                throw new ArgumentOutOfRangeException($"Index out of range: x={x}, z={z}");
            }
        }
    }

    
    public void rotate(Quaternion quaternion)
    {
        int yAngle = Mathf.RoundToInt(quaternion.eulerAngles.y / 90f) * 90;
        int normalizedAngle = ((yAngle % 360) + 360) % 360;

        int newSizeX = sizeX;
        int newSizeZ = sizeZ;

        // 90 / 270 회전이면 가로/세로가 뒤집힘
        if (normalizedAngle == 90 || normalizedAngle == 270)
        {
            newSizeX = sizeZ;
            newSizeZ = sizeX;
        }

        BlockMap result = new BlockMap("copy", newSizeX, newSizeZ);

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                BlockInfo srcBlockInfo = this[x, z];
                BlockInfo rotatedBlockInfo = null;

                if (srcBlockInfo != null)
                {
                    rotatedBlockInfo = srcBlockInfo.deepCopyWithRotate(quaternion);
                }

                int dstX = x;
                int dstZ = z;

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

                result[dstX, dstZ] = rotatedBlockInfo;
            }
        }

        // 현재 BlockMap 인스턴스의 크기/중심/배열 갱신
        sizeX = newSizeX;
        sizeZ = newSizeZ;

        float centerX = (sizeX - 1) / 2f;
        float centerZ = (sizeZ - 1) / 2f;
        center = new Vector2(centerX, centerZ);

        oneDMap = result.oneDMap;
    }

    private Vector2Int rotatePositionFromCenter(Vector2Int target, float sinAngle, float cosAngle)
    {
        // 상대 좌표 계산
        Vector2 relativePos = target - center;

        // 회전 행렬 적용
        float newX = relativePos.x * cosAngle - relativePos.y * sinAngle;
        float newZ = relativePos.x * sinAngle + relativePos.y * cosAngle;

        // 회전된 좌표 복원
        Vector2 newPosition = new Vector2(newX, newZ) + center;

        // 결과를 정수형 좌표로 변환하여 반환
        return new Vector2Int(Mathf.RoundToInt(newPosition.x), Mathf.RoundToInt(newPosition.y));
    }
}

[System.Serializable]
public class BlockInfo : IDeepCopyable<BlockInfo>
{
    public bool isWall;
    public bool isObject;
    public Quaternion quaternion;
    public Vector3 offset;
    public int id = -1;
    public List<YAxisBlock> yAxisBlockList = new List<YAxisBlock>();
    
    //맵생성이후 변화 데이터
    public bool createWallLock;
    public bool createFloorLock;
    public List<YAxisBlock> addedYAxisBlockList = new List<YAxisBlock>();
    
    public BlockInfo deepCopy()
    {
        BlockInfo blockInfo = new BlockInfo();
        
        blockInfo.isWall = isWall;
        blockInfo.isObject = isObject;
        blockInfo.id = id;
        blockInfo.offset = offset;
        blockInfo.quaternion = quaternion;
        
        if(yAxisBlockList.Count > 0)
            blockInfo.yAxisBlockList = new List<YAxisBlock>(yAxisBlockList);

        
        // blockInfo.createFloorLock = createFloorLock;
        // blockInfo.createWallLock = createWallLock;
        if (addedYAxisBlockList.Count > 0)
             blockInfo.addedYAxisBlockList = new List<YAxisBlock>(addedYAxisBlockList);
        

        return blockInfo;
    }
    
    public BlockInfo deepCopyWithRotate(Quaternion quaternion)
    {
        BlockInfo blockInfo = new BlockInfo();
        blockInfo.isWall = isWall;
        blockInfo.yAxisBlockList = rotateYAxisBlocks(yAxisBlockList,quaternion);
        blockInfo.isObject = isObject;
        blockInfo.id = id;
        blockInfo.offset = quaternion * offset;
        blockInfo.quaternion = addYAngle(quaternion,this.quaternion);
        
        blockInfo.addedYAxisBlockList = rotateYAxisBlocks(addedYAxisBlockList,quaternion);

        return blockInfo;
    }
    
    public void rotate(Quaternion quaternion)
    {
        yAxisBlockList = rotateYAxisBlocks(yAxisBlockList, quaternion);
        offset = quaternion * offset;
        this.quaternion = addYAngle(quaternion,this.quaternion);
        
        addedYAxisBlockList = rotateYAxisBlocks(addedYAxisBlockList,quaternion);
    }
    
    Quaternion addYAngle(Quaternion qA, Quaternion qB)
    {
        Vector3 ea = qA.eulerAngles;
        Vector3 eb = qB.eulerAngles;

        float newY = ea.y + eb.y; // 인스펙터에서 Y만 증가시키는 것과 동일
        return Quaternion.Euler(ea.x, newY, ea.z);
    }

    List<YAxisBlock> rotateYAxisBlocks(List<YAxisBlock> yAxisBlockList,Quaternion quaternion)
    {
        if (yAxisBlockList.Count > 0)
        {
            List<YAxisBlock> newYAxisBlocks = new List<YAxisBlock>();
            foreach (var yAxisBlock in yAxisBlockList)
            {
                newYAxisBlocks.Add(yAxisBlock.rotate(quaternion));
            }
            return newYAxisBlocks;
        }
        return new List<YAxisBlock>();
    }
}

[System.Serializable]
public struct YAxisBlock
{
    public int id;
    public Vector3 offset;
    public Quaternion quaternion;

    public YAxisBlock(int id, Quaternion quaternion,Vector3 offset=new Vector3())
    {
        this.id = id;
        this.offset = offset;
        this.quaternion = quaternion;
    }

    public YAxisBlock rotate(Quaternion quaternion)
    {
        Vector3 ea = this.quaternion.eulerAngles;
        Vector3 eb = quaternion.eulerAngles;
        float newY = ea.y + eb.y;
        
        return new YAxisBlock(id,Quaternion.Euler(ea.x, newY, ea.z),quaternion * offset);
    }
}