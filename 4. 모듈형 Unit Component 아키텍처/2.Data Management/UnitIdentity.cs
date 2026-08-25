using System;
using UnityEngine;

[Serializable]
public class UnitIdentity
{
    [SerializeField] private UnitIdentityData data = new UnitIdentityData();
    [NonSerialized] private SaverForData<UnitIdentityData> saver;
    [NonSerialized] private bool persistenceEnabled;

    public string name => data.name;
    public Disposition characterDisposition => data.characterDisposition;
    public UnitClass unitClass => data.unitClass;
    public bool isBrave => data.isBrave;

    public void initialize(MonoBehaviour owner)
    {
        saver = new SaverForData<UnitIdentityData>(data);
        saver.initializeSaver($"{data.name}-IdentityData", false);
        persistenceEnabled = true;
    }

    public void setName(string value)
    {
        data.name = value;
        save();
    }

    public void setDisposition(Disposition value)
    {
        data.characterDisposition = value;
        save();
    }

    public void setUnitClass(UnitClass value)
    {
        data.unitClass = value;
        save();
    }

    public void setBrave(bool value)
    {
        data.isBrave = value;
        save();
    }

    public UnitIdentityData createSnapshot()
    {
        UnitIdentityData result = new UnitIdentityData();
        result.copyFrom(data);
        return result;
    }

    public void applySnapshot(UnitIdentityData source)
    {
        data.copyFrom(source);
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
