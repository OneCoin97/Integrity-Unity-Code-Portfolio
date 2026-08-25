using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FOV 스트리밍 갱신 요청 데이터입니다.
/// 실제 프로젝트의 MapAssistClass에서 해당 구조체만 발췌했습니다.
/// </summary>
[Serializable]
public struct MapFormationMaterial
{
    public Fov fov;
    public List<Vector3Int> customArea;
    public bool isDelete;
    public bool isDestroy;

    public MapFormationMaterial(Fov fov, List<Vector3Int> customArea = null,
        bool isDelete = false, bool isDestroy = false)
    {
        this.fov = fov;
        this.customArea = customArea;
        this.isDelete = isDelete;
        this.isDestroy = isDestroy;
    }
}

/// <summary>
/// 한 번의 FOV 계산으로 결정된 View 생성·제거 목록입니다.
/// 계산이 끝난 뒤 HashSet은 HashSetPool로 반환됩니다.
/// </summary>
public readonly struct MapStreamingChanges
{
    public HashSet<Vector3Int> createdBlocks { get; }
    public HashSet<Vector3Int> destroyedBlocks { get; }

    public MapStreamingChanges(HashSet<Vector3Int> createdBlocks,
        HashSet<Vector3Int> destroyedBlocks)
    {
        this.createdBlocks = createdBlocks;
        this.destroyedBlocks = destroyedBlocks;
    }
}
