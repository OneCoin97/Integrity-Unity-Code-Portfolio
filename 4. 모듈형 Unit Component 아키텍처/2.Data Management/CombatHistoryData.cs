using System;

[Serializable]
public class CombatHistoryData
{
    public float copiedLastDealingAmount;
    public float copiedLastHealingAmount;
    public float copiedLastHealingReceived;
    public float copiedLastDealingReceived;

    public float lastDealingAmount;
    public float lastHealingAmount;
    public float lastHealingReceived;
    public float lastDealingReceived;

    public float dealingAmount;
    public float healingAmount;
    public float receivedHeal;
    public float receivedDeal;
    public float utility;

    public bool isFullHP;
    public bool isMoved;
    public int killCount;
    public float lastStamina;

    public float confirmedMoveRange;
    public float tempMoveRange;

    public CombatHistoryData deepCopy()
    {
        return (CombatHistoryData)MemberwiseClone();
    }
}
