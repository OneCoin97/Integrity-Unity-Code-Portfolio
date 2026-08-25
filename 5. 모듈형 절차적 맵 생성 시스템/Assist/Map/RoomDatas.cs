using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomDatas : IDeepCopyable<RoomDatas>
{
    public List<RoomData> data = new();
    public RoomDatas deepCopy()
    {
        RoomDatas result = new RoomDatas();
        
        foreach (var roomData in data)
        {
            result.data.Add(roomData.deepCopy());
        }

        return result;
    }

    public void merge(RoomDatas roomDatas,Vector3Int pos,RotateHelper rotateHelper)
    {
        RoomDatas copy = roomDatas.deepCopy();
        copy.rotate(rotateHelper);
        copy.move(pos);

        this.data.AddRange(copy.data);
    }
    
    public void rotate(RotateHelper rotateHelper)
    {
        foreach (var roomData in data)
        {
            roomData.rotate(rotateHelper);
        }
    }

    public void move(Vector3Int pos)
    {
        foreach (var roomData in data)
        {
            roomData.move(pos);
        }
    }
}



[Serializable]
public class RoomData: IDeepCopyable<RoomData>
{
    public RoomType roomType = RoomType.Combat;
    public RoomCombatType roomCombatType = RoomCombatType.기본;
    
    public Vector3 position;
    public Quaternion quaternion;
    public Vector3Int startPoint;
    public Vector3Int endPoint;
    public Vector2 size;

    public bool skillUpgrade;
    public float spawnDistance = 10;
    public float enemySpacing = 5;
    
    public Vector3Int initializePosition = -Vector3Int.one;
    public string subtitleKey;


    public RoomData deepCopy()
    {
        RoomData result = new RoomData();

        // Value / Unity structs (copy by value)
        result.roomType = roomType;
        result.roomCombatType = roomCombatType;
        result.position = position;
        result.quaternion = quaternion;
        result.startPoint = startPoint;
        result.endPoint = endPoint;
        result.size = size;

        result.skillUpgrade = skillUpgrade;
        result.spawnDistance = spawnDistance;
        result.enemySpacing = enemySpacing;

        result.initializePosition = initializePosition;
        result.subtitleKey = subtitleKey;
        
        return result;
    }

    public RoomData()
    {
        
    }
    
    public RoomData(Vector3 position,Quaternion quaternion, Vector2 size, int enterOffset)
    {
        Vector2 halfSize = size / 2;

        // ✅ startPoint = 중심 - 절반 크기 (FloorToInt로 정수 변환)
        startPoint = new Vector3Int(
            Mathf.FloorToInt(position.x - halfSize.x),
            Mathf.FloorToInt(position.y), // 높이는 그대로 유지
            Mathf.FloorToInt(position.z - halfSize.y) // Z축은 size.y 사용
        );

        // ✅ endPoint = 중심 + 절반 크기
        endPoint = new Vector3Int(
            Mathf.FloorToInt(position.x + halfSize.x),
            Mathf.FloorToInt(position.y),
            Mathf.FloorToInt(position.z + halfSize.y)
        );
        this.quaternion = quaternion;
        this.size = size - Vector2.one * enterOffset*2;
        if (this.size.x <= 1)
        {
            this.size.x = 1;
        }

        if (this.size.y <= 1)
        {
            this.size.y = 1;
        }
        
        
        this.position = position;
        this.position.y = 1; // Room은 단일 레이어 구조로 Y=1에 고정(지형 고도 개념 없음)
    }

    public void move(Vector3Int position)
    {
        this.position += position; 
        startPoint += position;
        endPoint += position;
    }

    public void rotate(RotateHelper rotateHelper)
    {
        startPoint = rotateHelper.rotate(startPoint);
        endPoint = rotateHelper.rotate(endPoint);
        position = rotateHelper.rotate(position);
        quaternion = rotateHelper.rotate(quaternion);
        
    }

  
}