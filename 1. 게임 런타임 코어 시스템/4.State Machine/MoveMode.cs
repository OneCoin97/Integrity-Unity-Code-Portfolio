namespace AdventureMode
{
    public class Move : GameMode<AdventureTurn>
    {
        public Move(System.Action<AdventureTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            if (!UnitSelectionManager.GetInst.tryGetSelectedUnit(out Unit selectedUnit)) return;

            selectedUnit.combatAttributes.resetStamina();
            if (selectedUnit.unitController.isCastingMove())
            {
                changeMode(AdventureTurn.Skill);
            }
        }
    }

    public class Skill : GameMode<AdventureTurn>
    {
        public Skill(System.Action<AdventureTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            if (!UnitSelectionManager.GetInst.tryGetSelectedUnit(out Unit selectedUnit)) return;

            if (!selectedUnit.unitController.isCastingMove())
            {
                changeMode(AdventureTurn.Move);
            }
        }
    }

    public class Load : GameMode<AdventureTurn>
    {
        public Load(System.Action<AdventureTurn> changeModeAction) : base(changeModeAction) { }

        protected override void evaluateTransition()
        {
            changeMode(AdventureTurn.Move);
        }
    }
}
