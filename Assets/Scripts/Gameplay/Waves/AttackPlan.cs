namespace Gatekeeper
{
    public enum ArmyType
    {
        GruntMix = 0,
        RunnerRush = 1,
        ShieldWall = 2,
        AirRaid = 3,
    }

    public enum LaneFocus
    {
        Left = 0,
        Middle = 1,
        Right = 2,
        Even = 3,
    }

    /// <summary>An attack plan: which army to send and where to send it.</summary>
    public readonly struct AttackPlan
    {
        public readonly ArmyType Army;
        public readonly LaneFocus Focus;

        public AttackPlan(ArmyType army, LaneFocus focus)
        {
            Army = army;
            Focus = focus;
        }

        public override string ToString() => PlanCatalog.ArmyName(Army) + " / " + PlanCatalog.FocusName(Focus);
    }

    /// <summary>The 16 attack plans, numbered 0..15.</summary>
    public static class PlanCatalog
    {
        public const int ArmyCount = 4;
        public const int FocusCount = 4;
        public const int Count = ArmyCount * FocusCount;

        public static AttackPlan FromIndex(int index) =>
            new AttackPlan((ArmyType)(index / FocusCount), (LaneFocus)(index % FocusCount));

        public static int ToIndex(AttackPlan plan) => (int)plan.Army * FocusCount + (int)plan.Focus;

        public static string ArmyName(ArmyType army)
        {
            switch (army)
            {
                case ArmyType.GruntMix: return "Grunt Mix";
                case ArmyType.RunnerRush: return "Runner Rush";
                case ArmyType.ShieldWall: return "Shield Wall";
                case ArmyType.AirRaid: return "Air Raid";
                default: return army.ToString();
            }
        }

        public static string FocusName(LaneFocus focus) =>
            focus == LaneFocus.Even ? "Even split" : MapConstants.LaneNames[(int)focus] + " lane";
    }
}
