using UnityEngine;

[System.Serializable]
public class Map
{
    public int seed;
    public BlockMap blockMap;
    public TriggerMap triggerMap;
    public RoomDatas roomDatas;
   

    public Map(BlockMap blockMap,TriggerMap triggerMap,RoomDatas roomDatas)
    {
        this.blockMap = blockMap;
        this.triggerMap = triggerMap;
        this.roomDatas = roomDatas;
    }

    public void moveWithoutBlockMap(Vector3Int bottomLeft)
    {
        roomDatas.move(-bottomLeft);
        triggerMap.move(-bottomLeft);
    }

    public void setBlockMap(BlockMap blockMap, Vector3Int bottomLeft)
    {
        this.blockMap = blockMap;
        roomDatas.move(-bottomLeft);
        triggerMap.move(-bottomLeft);
        
    }

    public void merge(Map otherMap,Vector3Int pos, Quaternion quaternion)
    {
        if (otherMap != null)
        {
            RotateHelper rotateHelper = new RotateHelper(otherMap.blockMap.sizeX, otherMap.blockMap.sizeZ, quaternion);
            roomDatas.merge(otherMap.roomDatas,pos,rotateHelper);
            triggerMap.merge(otherMap.triggerMap,pos,rotateHelper);
        }
    }
    

    public Map()
    {
        blockMap = new ("",0,0);
        triggerMap = new ();
        roomDatas = new ();
    }
}