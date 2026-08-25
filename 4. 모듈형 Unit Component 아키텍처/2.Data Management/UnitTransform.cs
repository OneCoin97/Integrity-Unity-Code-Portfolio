using System;
using UnityEngine;

[Serializable]
public class UnitTransform
{
    [SerializeField] private UnitTransformData data = new UnitTransformData();
    [NonSerialized] private SaverForData<UnitTransformData> saver;
    [NonSerialized] private bool persistenceEnabled;

    public Vector3 position => data.position;
    public Quaternion rotation => data.quaternion;
    public VisibleState visibleState => data.visibleState;

    public void initialize(MonoBehaviour owner, string unitName)
    {
        saver = new SaverForData<UnitTransformData>(data);
        saver.initializeSaver($"{unitName}-TransformData", false);
        persistenceEnabled = true;
    }

    public void setPosition(Vector3 value)
    {
        data.position = value;
        save();
    }

    public void setRotation(Quaternion value)
    {
        data.quaternion = value;
        save();
    }

    public void setVisibleState(VisibleState value)
    {
        data.visibleState = value;
        save();
    }

    public UnitTransformData createSnapshot()
    {
        UnitTransformData result = new UnitTransformData();
        result.copyFrom(data);
        return result;
    }

    public void applySnapshot(UnitTransformData source)
    {
        data.copyFrom(source);
    }

    public void capture(Transform target)
    {
        data.position = target.position;
        data.quaternion = target.rotation;
        if (data.position.y < 0.4f)
        {
            data.position.y = 0.5f;
        }

        save();
    }

    public void apply(Transform target)
    {
        target.position = data.position;
        target.rotation = data.quaternion;
    }

    public void load()
    {
        saver.loadImmediate();
        data.copyFrom(saver.data);
        saver.data = data;
    }

    public void save()
    {
        if (!persistenceEnabled)
        {
            return;
        }

        saver.data = data;
        saver.save();
    }

    public void dispose()
    {
        if (persistenceEnabled)
        {
            saver.removeSaver();
        }
    }
}
