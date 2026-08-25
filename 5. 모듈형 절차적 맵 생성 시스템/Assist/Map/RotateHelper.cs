using UnityEngine;

public class RotateHelper
{
    private int sizeX;
    private int sizeZ;
    private int normalizedAngle;
    private Quaternion quaternion;

    public RotateHelper(int sizeX, int sizeZ,Quaternion quaternion)
    {
        this.sizeX = sizeX;
        this.sizeZ = sizeZ;
        this.quaternion = quaternion;
        int yAngle = Mathf.RoundToInt(quaternion.eulerAngles.y / 90f) * 90;
        normalizedAngle = ((yAngle % 360) + 360) % 360;
    }

    public Quaternion rotate(Quaternion quaternion)
    {
        Vector3 ea = quaternion.eulerAngles;
        Vector3 eb = this.quaternion.eulerAngles;

        float newY = ea.y + eb.y;
        
        //[전제] 본 프로젝트의 맵/블록은 항상 수평으로 유지된다(기울기 없음)
        //따라서 회전 합성은 yaw(Y)만 적용하며 pitch/roll(X/Z)은 의도적으로 무시한다
        return Quaternion.Euler(eb.x, newY, eb.z);
    }

    public Vector3 rotate(Vector3 pos)
    {
        Vector2 result = rotate(new Vector2(pos.x,pos.z));

        return new Vector3(result.x, pos.y, result.y);
    }

    public Vector3Int rotate(Vector3Int pos)
    {
        Vector2Int result = rotate(new Vector2Int(pos.x,pos.z));

        return new Vector3Int(result.x, pos.y, result.y);
    }
    
    public Vector2Int rotate(Vector2Int pos)
    {
        int x = pos.x;
        int z = pos.y;

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

        return new Vector2Int(dstX, dstZ);
    }
    
    public Vector2 rotate(Vector2 pos)
    {
        float x = pos.x;
        float z = pos.y;

        float dstX = x;
        float dstZ = z;

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

        return new Vector2(dstX, dstZ);
    }
}