using System;

[Serializable]
public class UnitRuntimeData
{
    public float moveSpeed = 5f;
    public bool isDead;
    public bool castingSkill;
    public bool runSkill;
    public bool running;
    public bool isMove;
    public bool moveLock;
    public bool isKnockBack;
    public bool rush;
    public float currentSkillCost;
    public bool skillAdditionalInput;
}
