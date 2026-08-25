using System.Collections.Generic;
using UnityEngine;

public sealed class TemporaryAreaFov : Fov
{
    private Vector3Int currentPosition;

    public bool isEnd { get; set; }
    public Vector3 getCurrentPosition() => currentPosition;

    public int creationRange { get; set; }

    public void initialize(List<Vector3Int> area)
    {
        isEnd = false;
        creationRange = getFovValue(area, out Vector3Int center);
        currentPosition = center;
    }

    private int getFovValue(List<Vector3Int> area, out Vector3Int center)
    {
        int fov;

        if (area == null || area.Count == 0)
        {
            center = -Vector3Int.one;
            return 0;
        }

        // 초기 최소값 및 최대값 설정
        Vector3Int min = area[0];
        Vector3Int max = area[0];

        // 각 벡터를 검사하여 최소값 및 최대값 갱신
        foreach (var vector in area)
        {
            if (vector.x < min.x) min.x = vector.x;
            if (vector.z < min.z) min.z = vector.z;

            if (vector.x > max.x) max.x = vector.x;
            if (vector.z > max.z) max.z = vector.z;
        }

        // FOV 값을 계산
        Vector3Int fovVector3Int = max - min;
        fov = fovVector3Int.z > fovVector3Int.x ? fovVector3Int.z : fovVector3Int.x;

        // 센터 값을 계산
        center = new Vector3Int((min.x + max.x) / 2, (min.y + max.y) / 2, (min.z + max.z) / 2);

        return fov;
    }
    
}
