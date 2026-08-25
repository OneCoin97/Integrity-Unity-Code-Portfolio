namespace EnemyTurn
{
    public class Delay : GameMode<CombatTurn>
    {
        public Delay(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.EReady);
        }
    }

    public class Ready : GameMode<CombatTurn>
    {
        public Ready(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.EMove);
        }
    }

    public class Move : GameMode<CombatTurn>
    {
        public Move(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            if (!UnitSelectionManager.GetInst.tryGetSelectedUnit(out Unit selectedUnit)) return;

            if (selectedUnit.unitController.isCastingMove())
            {
                changeMode(CombatTurn.ESkill);
            }
        }
    }

    public class Skill : GameMode<CombatTurn>
    {
        public Skill(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            if (!UnitSelectionManager.GetInst.tryGetSelectedUnit(out Unit selectedUnit)) return;

            if (!selectedUnit.unitController.isCastingMove())
            {
                changeMode(CombatTurn.EMove);
            }
        }
    }

    public class Wait : GameMode<CombatTurn>
    {
        public Wait(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.Delay);
        }
    }
    
}
