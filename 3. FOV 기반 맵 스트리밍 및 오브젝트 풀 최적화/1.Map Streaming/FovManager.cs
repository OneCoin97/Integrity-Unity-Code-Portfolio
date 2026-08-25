using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 FOV가 점유하는 블록 영역을 통합하고 생성·제거 변경분을 계산합니다.
/// </summary>
public class FovManager : MonoBehaviour
{
    private sealed class FovStreamingState : IClassPoolEntry
    {
        public Vector3Int lastUpdatePosition;
        public readonly HashSet<Vector3Int> coordinates = new();

        public FovStreamingState()
        {
        }

        public void onRent()
        {
            reset();
        }

        public void onReturn()
        {
            reset();
        }

        private void reset()
        {
            lastUpdatePosition = default;
            coordinates.Clear();
        }
    }

    private readonly HashSetPool<Vector3Int> v3IntHashSetPool = new();
    private readonly Dictionary<Fov, FovStreamingState> fovStates = new();
    private readonly ClassPool<FovStreamingState> fovStatePool = new();

    private int actorCreationRange;
    private Vector3 actorPosition;
    private Vector3Int beforePosition;
    private readonly HashSet<Fov> fovHash = new();
    
    private Fov actor;
    private FovStreamingState actorState;
    private MapModel mapModel;

    private void Awake()
    {
        mapModel = GetComponent<MapModel>();
    }

    public List<Fov> getFovSnapshot()
    {
        return new List<Fov>(fovHash);
    }

    private void addFov(Fov fov)
    {
        fovHash.Add(fov);
    }

    public void removeFov(Fov fov)
    {
        fovHash.Remove(fov);

        if (!fovStates.Remove(fov, out FovStreamingState state))
            return;

        fovStatePool.Release(state);

        if (ReferenceEquals(actor, fov))
            actorState = null;
    }

    public void initialize()
    {
        foreach (FovStreamingState state in fovStates.Values)
            fovStatePool.Release(state);

        fovStates.Clear();
        fovHash.Clear();
        actor = null;
        actorState = null;
    }

    public void setActor(Fov fov, Vector3 position, bool remove = false)
    {
        position.y = 0;
        actor = fov;
        actorCreationRange = remove ? 0 : Mathf.Clamp(fov.creationRange, 6, 20);
        addFov(fov);
        actorPosition = position;
        actorState = getOrCreateState(fov, position);
        beforePosition = actorState.lastUpdatePosition;
        actorState.lastUpdatePosition = Vector3Int.RoundToInt(position);
    }

    private FovStreamingState getOrCreateState(Fov fov, Vector3 position)
    {
        if (fovStates.TryGetValue(fov, out FovStreamingState state))
            return state;

        Vector3Int initialPosition = Vector3Int.RoundToInt(position);
        initialPosition.y = 0;

        state = fovStatePool.Get();
        state.lastUpdatePosition = initialPosition;
        fovStates.Add(fov, state);
        return state;
    }

    HashSet<Vector3Int> findOverlappingFov()
    {
        HashSet<Vector3Int> overlapped = v3IntHashSetPool.Get();

        // Y 축은 무시하고 그리드 좌표로 변환
        Vector3Int actorGridPos = Vector3Int.RoundToInt(actorPosition);
        actorGridPos.y = 0;
        int movedRange = (int)(actorState.lastUpdatePosition - beforePosition).magnitude + 1;
      
        foreach (var fov in fovHash)
        {
            if (ReferenceEquals(fov, actor) ||
                !fovStates.TryGetValue(fov, out FovStreamingState state))
                continue;

            int combinedRange = actor.creationRange + fov.creationRange + 2 + movedRange;

            Vector3Int targetGridPos = state.lastUpdatePosition;

            int dx = Mathf.Abs(actorGridPos.x - targetGridPos.x);
            int dz = Mathf.Abs(actorGridPos.z - targetGridPos.z);

            if (dx <= combinedRange && dz <= combinedRange)
            {
                overlapped.UnionWith(state.coordinates);
            }

        }

        return overlapped;
    }

    HashSet<Vector3Int> makeActorArea()
    {
        HashSet<Vector3Int> createdThings = v3IntHashSetPool.Get();

        if (actorCreationRange > 0)
        {
            int xLength = mapModel.blockMap.sizeX - 1;
            int zLength = mapModel.blockMap.sizeZ - 1;

            int xMin = checkArrayIndex((int)actorPosition.x - actorCreationRange, xLength);
            int xMax = checkArrayIndex((int)actorPosition.x + actorCreationRange, xLength);
            int zMin = checkArrayIndex((int)actorPosition.z - actorCreationRange, zLength);
            int zMax = checkArrayIndex((int)actorPosition.z + actorCreationRange, zLength);

            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    createdThings.Add(new Vector3Int(x, 0, z));
                }
            }
        }

        return createdThings;
    }

    public MapStreamingChanges setRange(List<Vector3Int> actorArea = null)
    {
        HashSet<Vector3Int> actorFovCoordinates = actorState.coordinates;
        HashSet<Vector3Int> wholeMap = findOverlappingFov();

        HashSet<Vector3Int> createdAllSet;
        if (actorArea == null)
        {
            createdAllSet = makeActorArea();
        }
        else
        {
            createdAllSet = v3IntHashSetPool.Get(actorArea);
        }

        HashSet<Vector3Int> createdBlocks = v3IntHashSetPool.Get(createdAllSet);
        
        createdBlocks.ExceptWith(actorFovCoordinates);
        createdBlocks.ExceptWith(wholeMap);
        
        HashSet<Vector3Int> keepSet = v3IntHashSetPool.Get(wholeMap);
        keepSet.UnionWith(createdAllSet);

        HashSet<Vector3Int> destroyedBlocks = v3IntHashSetPool.Get(actorFovCoordinates);
        destroyedBlocks.ExceptWith(keepSet);
        
        actorFovCoordinates.Clear();
        actorFovCoordinates.UnionWith(createdAllSet);
        v3IntHashSetPool.Release(createdAllSet);
        v3IntHashSetPool.Release(wholeMap);
        v3IntHashSetPool.Release(keepSet);

        return new MapStreamingChanges(createdBlocks, destroyedBlocks);
    }

    public void releaseMapChanges(MapStreamingChanges changes)
    {
        v3IntHashSetPool.Release(changes.createdBlocks);
        v3IntHashSetPool.Release(changes.destroyedBlocks);
    }

 
    /// <summary>
    /// 배열의 인덱스를 초과하는지 확인해줌
    /// </summary>
    /// <param name="tA">인덱스값</param>
    /// <param name="length">인덱스 최대값</param>
    /// <returns></returns>
    int checkArrayIndex(int tA, int length)
    {
        if (tA < 0)
        {
            return 0;
        }

        if (tA > length)
        {
            return length;
        }

        return tA;
    }
}
