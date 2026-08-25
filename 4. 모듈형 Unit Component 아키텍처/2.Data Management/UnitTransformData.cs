using System;
using UnityEngine;

[Serializable]
public class UnitTransformData
{
    public Vector3 position = -Vector3.one;
    public Quaternion quaternion = Quaternion.identity;
    public VisibleState visibleState;

    public void copyFrom(UnitTransformData source)
    {
        position = source.position;
        quaternion = source.quaternion;
        visibleState = source.visibleState;
    }
}
