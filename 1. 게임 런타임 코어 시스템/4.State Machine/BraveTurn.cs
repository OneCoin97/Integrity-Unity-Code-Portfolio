namespace BraveTurn
{
    public class None : GameMode<CombatTurn>
    {
        public None(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.Delay);
        }
    }

    public class Delay : GameMode<CombatTurn>
    {
        public Delay(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.Ready);
        }
    }

    public class Ready : GameMode<CombatTurn>
    {
        public Ready(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.Move);
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
                changeMode(CombatTurn.Skill);
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
                changeMode(CombatTurn.Move);
            }
        }
    }

    public class Wait : GameMode<CombatTurn>
    {
        public Wait(System.Action<CombatTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(CombatTurn.EDelay);
        }
    }


}
