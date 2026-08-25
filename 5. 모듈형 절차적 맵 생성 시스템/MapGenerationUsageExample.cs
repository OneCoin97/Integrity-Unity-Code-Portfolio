using System.Collections;
using MapBuild;
using UnityEngine;

[RequireComponent(typeof(MapLayoutGenerator), typeof(MapBuilder))]
public class MapGenerationUsageExample : MonoBehaviour
{
    // MapLayoutGenerator의 defaultMapData에는 완성된 Map JSON 파일을 연결한다.
    private MapLayoutGenerator layoutGenerator;
    private MapBuilder mapBuilder;

    public Map result { get; private set; }

    private void Awake()
    {
        layoutGenerator = GetComponent<MapLayoutGenerator>();
        mapBuilder = GetComponent<MapBuilder>();
    }

    public IEnumerator CreateMap(int stage)
    {
        layoutGenerator.setCOption(stage);
        yield return layoutGenerator.makeMapData();

        if (layoutGenerator.fallbackMap != null)
        {
            result = layoutGenerator.fallbackMap;
            yield break;
        }

        if (layoutGenerator.cData == null)
        {
            Debug.LogError("Map layout generation failed and no fallback map is available.");
            yield break;
        }

        mapBuilder.setLayoutData(layoutGenerator.cData, stage);
        yield return mapBuilder.startMakingMap();
        result = mapBuilder.getMap();
    }
}
