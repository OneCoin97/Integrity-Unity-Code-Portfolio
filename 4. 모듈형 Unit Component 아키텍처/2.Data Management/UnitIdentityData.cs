using System;

[Serializable]
public class UnitIdentityData
{
    public string name = "unit";
    public Disposition characterDisposition;
    public UnitClass unitClass = UnitClass.Null;
    public bool isBrave;

    public void copyFrom(UnitIdentityData source)
    {
        name = source.name;
        characterDisposition = source.characterDisposition;
        unitClass = source.unitClass;
        isBrave = source.isBrave;
    }
}
