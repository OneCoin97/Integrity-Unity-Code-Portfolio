using System;
using System.Collections.Generic;

[Serializable]
public class TargetHistoryData
{
    [NonSerialized] public Unit lastReceiver;

    public bool isSkillInvolved;

    public List<string> lastBraveTargets = new List<string>();
    public List<string> lastEnemyTargets = new List<string>();
    public List<string> targetedBraveList = new List<string>();
    public List<string> targetedEnemyList = new List<string>();
    public List<string> lastSkillBraveTargets = new List<string>();
    public List<string> lastSkillEnemyTargets = new List<string>();

    public List<string> lastBraveTargeter = new List<string>();
    public List<string> lastEnemyTargeter = new List<string>();
    public List<string> braveTargeterList = new List<string>();
    public List<string> enemyTargeterList = new List<string>();

    public List<string> copiedLastBraveTargets = new List<string>();
    public List<string> copiedLastEnemyTargets = new List<string>();
    public List<string> copiedLastBraveTargeter = new List<string>();
    public List<string> copiedLastEnemyTargeter = new List<string>();

    public TargetHistoryData deepCopy()
    {
        TargetHistoryData copy = new TargetHistoryData();
        copy.isSkillInvolved = isSkillInvolved;
        copy.lastBraveTargets = new List<string>(lastBraveTargets);
        copy.lastEnemyTargets = new List<string>(lastEnemyTargets);
        copy.targetedBraveList = new List<string>(targetedBraveList);
        copy.targetedEnemyList = new List<string>(targetedEnemyList);
        copy.lastSkillBraveTargets = new List<string>(lastSkillBraveTargets);
        copy.lastSkillEnemyTargets = new List<string>(lastSkillEnemyTargets);
        copy.lastBraveTargeter = new List<string>(lastBraveTargeter);
        copy.lastEnemyTargeter = new List<string>(lastEnemyTargeter);
        copy.braveTargeterList = new List<string>(braveTargeterList);
        copy.enemyTargeterList = new List<string>(enemyTargeterList);
        copy.copiedLastBraveTargets = new List<string>(copiedLastBraveTargets);
        copy.copiedLastEnemyTargets = new List<string>(copiedLastEnemyTargets);
        copy.copiedLastBraveTargeter = new List<string>(copiedLastBraveTargeter);
        copy.copiedLastEnemyTargeter = new List<string>(copiedLastEnemyTargeter);
        return copy;
    }
}
