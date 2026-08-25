using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 실제 MapLifecycleManager에서 맵 스트리밍 수명주기와
/// 스킬 경로용 임시 FOV 관리 부분을 발췌한 코드입니다.
/// </summary>
public class MapLifecycleStreamingExcerpt
{
    private MapFovUpdateManager fovUpdateManager;
    private FovManager fovManager;
    private MapViewBlockStreamingExcerpt viewBlock;
    private MapModel model;
    private readonly List<TemporaryAreaFov> temporaryFovs = new();

    /// <summary>
    /// 유닛 FOV 밖에 있는 스킬 경로도 물리 지형과 충돌할 수 있도록
    /// 스킬 영역을 임시 FOV로 등록하고 필요한 View를 즉시 생성합니다.
    /// </summary>
    public void makeInvisibleFov(List<Vector3Int> area)
    {
        if (area == null || area.Count == 0)
            return;

        TemporaryAreaFov temporaryFov = new TemporaryAreaFov();
        temporaryFov.initialize(area);

        fovUpdateManager.makingMapImmediately(
            new MapFormationMaterial(temporaryFov, area));

        temporaryFovs.Add(temporaryFov);
    }

    /// <summary>
    /// 임시 FOV가 끝나면 일반 FOV 제거 경로로 넘겨
    /// 다른 FOV가 유지하지 않는 영역만 스트리밍 해제합니다.
    /// </summary>
    public void clearTemporaryFovs()
    {
        foreach (TemporaryAreaFov temporaryFov in temporaryFovs)
        {
            if (temporaryFov != null)
                fovUpdateManager.deleteFov(temporaryFov, true);
        }

        temporaryFovs.Clear();
    }

    public void onAdventure()
    {
        clearTemporaryFovs();
    }

    public IEnumerator changeMap(Map map, bool withSave)
    {
        // 이전 맵의 모든 FOV에 제거 갱신을 요청한다.
        fovUpdateManager.deleteFovs(fovManager.getFovSnapshot(), true);

        // 요청 큐와 진행 중인 FOV 제거가 모두 끝날 때까지 기다린다.
        yield return null;
        yield return fovUpdateManager.waitForRunning();
        yield return null;

        // 활성 View와 비활성 풀, 진행 중인 비동기 반환을 함께 정리한다.
        viewBlock.destroyAllBlocks();

        fovManager.initialize();
        model.setMap(map, withSave);
    }
}
