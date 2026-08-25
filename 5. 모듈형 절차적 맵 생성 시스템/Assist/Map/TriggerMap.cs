using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TriggerMap
{
    public List<TriggerArea> map = new List<TriggerArea>();

    public void move(Vector3 position)
    {
        foreach (var triggerArea in map)
        {
            triggerArea.move(position);
        }
    }

    public void rotate(RotateHelper rotateHelper)
    {
        foreach (var triggerArea in map)
        {
            triggerArea.rotate(rotateHelper);
        }
    }

    public TriggerArea findTriggerArea(Vector3 position, TriggerAreaType triggerAreaType)
    {
        foreach (var triggerArea in map)
        {
            if (triggerArea.position == position && triggerArea.triggerAreaType == triggerAreaType)
            {
                return triggerArea;
            }
        }
        return null;
    }

    public bool remove(Vector3 position, TriggerAreaType triggerAreaType)
    {
        TriggerArea triggerArea = findTriggerArea(position, triggerAreaType);
        if (triggerArea != null)
        {
            map.Remove(triggerArea);
            return true;
        }

        return false;
    }
    
    public bool add(TriggerArea triggerArea)
    {
        TriggerArea existingTriggerArea = findTriggerArea(triggerArea.position, triggerArea.triggerAreaType);
        if (existingTriggerArea == null)
        {
            map.Add(triggerArea);
            return true;
        }

        return false;
    }

    public void merge(TriggerMap otherTriggerMap, Vector3 offset, RotateHelper rotateHelper)
    {
        foreach (var otherTriggerArea in otherTriggerMap.map)
        {
            // 깊은 복사
            TriggerArea copiedTriggerArea = otherTriggerArea.deepCopy();

            // 회전 적용
            copiedTriggerArea.rotate(rotateHelper);
            // 위치 이동
            copiedTriggerArea.move(offset);
            
            // 충돌 여부 확인
            TriggerArea existingTriggerArea = findTriggerArea(copiedTriggerArea.position, copiedTriggerArea.triggerAreaType);

            if (existingTriggerArea != null)
            {
                // 충돌 처리 (덮어쓰기)
                Debug.LogWarning($"병합 중 트리거 영역 충돌 발생: 위치={copiedTriggerArea.position}, 타입={copiedTriggerArea.triggerAreaType}");
                map.Remove(existingTriggerArea);
            }

            // 새로운 트리거 영역 추가
            map.Add(copiedTriggerArea);
        }
    }
}

[Serializable]
public class TriggerArea : IDeepCopyable<TriggerArea>
{
    public TriggerAreaType triggerAreaType;
    public bool isDefaultActive = true;
    [Header("CreateLocker")] 
    public bool wall;
    public bool floor;
    [Header("Camera")] 
    public CameraMode cameraMode;
    [Header("Activator")]
    public TriggerActivatorType activatorType;
    public GameModeType activatingMode = GameModeType.Adventure;
    public bool once;
    [Header("Dialogue")] 
    public DialoguePlace dialogueType;
    public bool random;
    public int dialogueID;
    [Header("BGM")]
    public SFXTriggerType sfxTriggerType;

    public int mID = -1;
    [Header("popup")] 
    public string pKey;

  
    [HideInInspector] public int id;
    public Vector3 position;
    public Vector2 size;
    [HideInInspector] public Quaternion quaternion;

    public TriggerArea deepCopy()
    {
        return new TriggerArea
        {
            triggerAreaType = this.triggerAreaType,
            isDefaultActive = this.isDefaultActive,

            wall = this.wall,
            floor = this.floor,

            cameraMode = this.cameraMode,

            activatorType = this.activatorType,
            activatingMode = this.activatingMode,
            once = this.once,

            dialogueType = this.dialogueType, 
            random = this.random,
            dialogueID = this.dialogueID,

            sfxTriggerType = this.sfxTriggerType,

            mID = this.mID,
            pKey = this.pKey,

            id = this.id,
            position = this.position,
            size = this.size,
            quaternion = this.quaternion
        };
    }


    public TriggerArea()
    {

    }

    public TriggerArea(int id, TriggerAreaType triggerAreaType, Vector3 position, Vector2 size)
    {
        this.id = id;
        this.triggerAreaType = triggerAreaType;
        this.position = position;
        this.size = size;
    }

    public void move(Vector3 position)
    {
        this.position += position;
    }

    public void rotate(RotateHelper rotateHelper)
    {
        position = rotateHelper.rotate(position);
        quaternion = rotateHelper.rotate(quaternion);
    }
    

}