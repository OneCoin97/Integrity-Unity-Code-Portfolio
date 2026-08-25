using System;
using System.Collections;
using System.Collections.Generic;
using ActivingObject;
using UnityEngine;

public partial class MapViewModel
{
    [Serializable]
    private sealed class MapFovUpdateManager
    {
        [NonSerialized] private MonoBehaviour coroutineHost;
        [NonSerialized] private MapViewBlock viewBlock;
        [NonSerialized] private MapModel model;
        [NonSerialized] private FovManager fovManager;
        
        [NonSerialized] private Queue<MapFormationMaterial> waitingList;
        [NonSerialized] private int runProcessCount;

        public bool running => runProcessCount > 0 || waitingList is { Count: > 0 };

        public void initialize(MonoBehaviour host, MapViewBlock mapViewBlock,
            MapModel mapModel, FovManager mapFovManager)
        {
            coroutineHost = host;
            viewBlock = mapViewBlock;
            model = mapModel;
            fovManager = mapFovManager;
            waitingList = new Queue<MapFormationMaterial>();
            runProcessCount = 0;
            coroutineHost.StartCoroutine(mapUpdateManager());
        }

        public IEnumerator waitForRunning()
        {
            yield return new WaitUntil(() => !running);
        }

        public void deleteFov(Fov fov, bool isDestroy)
        {
            makingMap(new MapFormationMaterial(fov, null, true, isDestroy));
        }

        public void deleteFovs(List<Fov> deleteThings, bool isDestroy)
        {
            foreach (var fov in deleteThings)
                makingMap(new MapFormationMaterial(fov, null, true, isDestroy));
        }

        public void makingMap(MapFormationMaterial mapFormationMaterial)
        {
            Fov fov = mapFormationMaterial.fov;
            if (fov == null || fov.isEnd)
                return;

            waitingList.Enqueue(mapFormationMaterial);
        }

        public void makingMapImmediately(MapFormationMaterial mapFormationMaterial)
        {
            Fov fov = mapFormationMaterial.fov;
            if (fov == null || fov.isEnd)
                return ;

            doMakingMap(mapFormationMaterial, fov.getCurrentPosition());
            if (mapFormationMaterial.isDestroy)
                coroutineHost.StartCoroutine(destroyFov(mapFormationMaterial));

        }

        private IEnumerator mapUpdateManager()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (waitingList.Count == 0)
                    continue;

                try
                {
                    MapFormationMaterial data = waitingList.Dequeue();
                    if (data.fov != null && !data.fov.isEnd)
                    {
                        doMakingMap(data, data.fov.getCurrentPosition());
                        if (data.isDestroy)
                            coroutineHost.StartCoroutine(destroyFov(data));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        private IEnumerator destroyFov(MapFormationMaterial data)
        {
            // 데이터형 FOV는 제거할 Unity 오브젝트나 전환 컴포넌트가 없다.
            if (data.fov is not Component fovComponent)
                yield break;

            const float transitionTimeout = 5f;

            try
            {
                runProcessCount++;
                yield return null;

                MapFovTransition[] transitions =
                    fovComponent.GetComponentsInChildren<MapFovTransition>();
                List<Coroutine> transitionCoroutines = new List<Coroutine>();
                int runningTransitions = 0;

                foreach (MapFovTransition transition in transitions)
                {
                    if (transition is UnityEngine.Object unityObject && unityObject == null)
                        continue;

                    runningTransitions++;
                    transitionCoroutines.Add(coroutineHost.StartCoroutine(
                        runTransition(transition.turnOff(), () => runningTransitions--)));
                }

                float timeoutAt = Time.realtimeSinceStartup + transitionTimeout;
                while (runningTransitions > 0 && Time.realtimeSinceStartup < timeoutAt)
                    yield return null;

                if (runningTransitions > 0)
                {
                    foreach (Coroutine transitionCoroutine in transitionCoroutines)
                        coroutineHost.StopCoroutine(transitionCoroutine);
                }

                yield return null;
                if (fovComponent != null)
                    UnityEngine.Object.Destroy(fovComponent.gameObject);
            }
            finally
            {
                runProcessCount--;
            }
        }

        private IEnumerator runTransition(IEnumerator transition, Action onComplete)
        {
            try
            {
                if (transition != null)
                    yield return transition;
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private void doMakingMap(MapFormationMaterial data, Vector3 actorPosition)
        {
            MapStreamingChanges changes = default;

            try
            {
                changes = calculateMapChanges(data, actorPosition);

                foreach (var position in changes.destroyedBlocks)
                    destroyMapBlock(position);
                
                foreach (var position in changes.createdBlocks)
                    createMapBlock(position);
            }
            finally
            {
                fovManager.releaseMapChanges(changes);
            }
        }
        
        private void destroyMapBlock(Vector3Int position)
        {
            BlockInfo blockInfo = model.blockMap[position.x, position.z];
            if (blockInfo == null)
                return;

            viewBlock.doDestroyBlock(position, false, false);

            foreach (var yAxisBlock in blockInfo.yAxisBlockList)
            {
                position += Vector3Int.up;
                if (yAxisBlock.id != -1)
                    viewBlock.doDestroyBlock(position, false, false);
            }

        }

        private void createMapBlock(Vector3Int position)
        {
            Vector3Int basePosition = position;
            BlockInfo blockInfo = model.blockMap[position.x, position.z];
            if (blockInfo == null)
                return;

            if (blockInfo.id != -1)
            {
                if (blockInfo.createFloorLock)
                {
                    viewBlock.doCreateLockBlock(position);
                }
                else if (blockInfo.isObject)
                {
                    viewBlock.doCreateBlock(model.blocks[blockInfo.id], position,
                        blockInfo.quaternion, blockInfo.offset, 0, false);
                }
                else
                {
                    viewBlock.doCreateBlock(model.blocks[blockInfo.id], position,
                        blockInfo.quaternion, blockInfo.offset);
                }
            }

            if (blockInfo.createWallLock)
            {
                foreach (var yAxisBlock in blockInfo.yAxisBlockList)
                {
                    position += Vector3Int.up;
                    if (yAxisBlock.id != -1)
                        viewBlock.doCreateLockBlock(position);
                }

                return;
            }

            foreach (var yAxisBlock in blockInfo.yAxisBlockList)
            {
                position += Vector3Int.up;
                if (yAxisBlock.id == -1)
                    continue;

                createYAxisBlock(position, yAxisBlock, false);
            }

            position = basePosition;
            foreach (var yAxisBlock in blockInfo.addedYAxisBlockList)
            {
                position += Vector3Int.up;
                if (yAxisBlock.id == -1)
                    continue;

                createYAxisBlock(position, yAxisBlock, true);
            }

        }

        private void createYAxisBlock(Vector3Int position, YAxisBlock yAxisBlock, bool isAdded)
        {
            Block block = model.blocks[yAxisBlock.id];
            int layer = block.gameObject.CompareTag(Tag.Decoration.ToString()) ? 20 : 3;
            viewBlock.doCreateBlock(block, position, yAxisBlock.quaternion, yAxisBlock.offset, layer,
                isAdded);
        }

        private MapStreamingChanges calculateMapChanges(MapFormationMaterial data, Vector3 actorPosition)
        {
            bool remove = data.isDelete || data.isDestroy;
            fovManager.setActor(data.fov, actorPosition, remove);

            MapStreamingChanges changes = fovManager.setRange(data.customArea);

            if (remove)
                fovManager.removeFov(data.fov);

            // 영구 파괴된 FOV에 뒤늦은 갱신 명령이 다시 적용되지 않도록 한다.
            // 상태 저장은 FOV가 담당하지만, 변경과 평가는 이 매니저에서만 수행한다.
            data.fov.isEnd = data.isDestroy;
            return changes;
        }

    }
}
