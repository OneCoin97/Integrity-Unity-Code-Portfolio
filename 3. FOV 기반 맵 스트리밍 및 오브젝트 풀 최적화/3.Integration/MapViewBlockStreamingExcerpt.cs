using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 MapViewBlock에서 블록 스트리밍과 오브젝트 풀 연동 부분만 발췌한 코드입니다.
/// Block과 관련 데이터 타입은 전체 프로젝트 코드에 포함됩니다.
/// </summary>
public class MapViewBlockStreamingExcerpt : MonoBehaviour
{
    private Dictionary<Vector3Int, Block> createdBlock;
    private Dictionary<Vector3Int, Block> addedCreatedBlock;

    private ObjectPoolManager<Block, int> blockObjectPool;
    private bool useRayfire;

    public void doCreateBlock(Block prefab, Vector3Int position, Quaternion rotation,
        Vector3 offset, int layer = 9, bool isAdded = false)
    {
        Dictionary<Vector3Int, Block> blocks = isAdded ? addedCreatedBlock : createdBlock;
        if (blocks.ContainsKey(position))
            return;

        Block instance = blockObjectPool.Instantiate(prefab);
        instance.setPosition(position, CheckOffset(offset), rotation);
        instance.setLayer(layer);
        instance.isAdded = isAdded;
        blocks[position] = instance;
    }

    public void doDestroyBlock(Vector3Int position, bool isAdded, bool rayFire)
    {
        Dictionary<Vector3Int, Block> blocks = isAdded ? addedCreatedBlock : createdBlock;
        if (!blocks.Remove(position, out Block block) || block == null)
            return;

        if (rayFire && useRayfire)
        {
            block.rayFire = true;
            blockObjectPool.Destroy(block);
            return;
        }

        StartCoroutine(block.destroyInArea(blockObjectPool));
    }

    public void destroyAllBlocks()
    {
        foreach (Block block in createdBlock.Values)
            blockObjectPool.Destroy(block);

        foreach (Block block in addedCreatedBlock.Values)
            blockObjectPool.Destroy(block);

        createdBlock.Clear();
        addedCreatedBlock.Clear();

        blockObjectPool.ClearAllData();
    }

    private static Vector3 CheckOffset(Vector3 offset)
    {
        return offset;
    }
}
