using System;
using System.Text;

namespace Gatekeeper.AI
{
    /// <summary>
    /// Turns the model's reasoning into short in-world text.
    /// When the policy is explainable, sentences come from the largest learned
    /// contributions theta_a[i] * x[i], so the report reflects what the model actually learned.
    /// </summary>
    public static class WarReportWriter
    {
        /// <summary>Banner shown when a wave starts.</summary>
        public static string Intent(int arm, DefenseSnapshot snapshot, float[] context, IWavePolicy policy)
        {
            var plan = PlanCatalog.FromArm(arm);
            var sb = new StringBuilder();
            sb.Append("The War Council sends a ").Append(PlanCatalog.ArmyName(plan.Army));
            sb.Append(plan.Focus == LaneFocus.Even
                ? " across every lane."
                : " at your " + MapConstants.LaneNames[(int)plan.Focus] + " lane.");

            string reason = null;
            if (policy is IExplainablePolicy explainable && explainable.UpdatesSeen > 0)
            {
                var contributions = TryContributions(explainable, arm, context);
                int best = contributions == null ? -1 : TopFeature(contributions, positive: true);
                if (best >= 0) reason = FeatureSentence(best, plan.Army, helpsEnemy: true);
            }

            if (reason == null && plan.Focus != LaneFocus.Even)
            {
                int count = snapshot.CountInLane((int)plan.Focus);
                reason = "Our scouts counted " + count + (count == 1 ? " defence" : " defences") + " there.";
            }

            if (reason != null) sb.Append('\n').Append(reason);
            return sb.ToString();
        }

        /// <summary>Report screen shown after a wave.</summary>
        public static string Debrief(int arm, WaveOutcome outcome, float reward, float[] context, IWavePolicy policy)
        {
            var plan = PlanCatalog.FromArm(arm);
            var sb = new StringBuilder();
            sb.Append("Plan used: ").Append(plan).Append('\n');
            sb.Append("Gate damage: ").Append(Percent(outcome.GateDamage, outcome.GateMaxHp));
            sb.Append("   Hero damage: ").Append(Percent(outcome.HeroDamage, outcome.HeroMaxHp)).Append('\n');
            sb.Append("Reached the gate: ").Append(outcome.ReachedGate).Append(" of ").Append(outcome.Spawned).Append('\n');
            sb.Append("Council reward: ").Append(reward.ToString("0.00")).Append("\n\n");

            if (policy is IExplainablePolicy explainable)
            {
                var contributions = TryContributions(explainable, arm, context);
                if (contributions == null)
                {
                    sb.Append("The Council's mind is still empty (LinUCB is not implemented yet).");
                    return sb.ToString();
                }

                int helps = TopFeature(contributions, positive: true);
                int hurts = TopFeature(contributions, positive: false);
                sb.Append("What the Council learned:\n");
                if (helps >= 0) sb.Append("+ ").Append(FeatureSentence(helps, plan.Army, helpsEnemy: true)).Append('\n');
                if (hurts >= 0) sb.Append("- ").Append(FeatureSentence(hurts, plan.Army, helpsEnemy: false)).Append('\n');
                if (helps < 0 && hurts < 0) sb.Append("Nothing stands out yet.\n");
            }
            else
            {
                sb.Append("The Council is not learning (" + policy.Name + " policy).");
            }

            return sb.ToString();
        }

        static float[] TryContributions(IExplainablePolicy policy, int arm, float[] context)
        {
            try { return policy.FeatureContributions(arm, context); }
            catch (NotImplementedException) { return null; }
        }

        /// <summary>Index of the largest positive (or most negative) non-bias contribution, or -1.</summary>
        static int TopFeature(float[] contributions, bool positive)
        {
            const float threshold = 0.01f;
            int best = -1;
            float bestValue = threshold;
            for (int i = 0; i < contributions.Length; i++)
            {
                if (ContextEncoder.KindOf(i) == ContextEncoder.FeatureKind.Bias) continue;
                float value = positive ? contributions[i] : -contributions[i];
                if (value > bestValue)
                {
                    bestValue = value;
                    best = i;
                }
            }
            return best;
        }

        public static string FeatureSentence(int feature, ArmyType army, bool helpsEnemy)
        {
            string armyName = PlanCatalog.ArmyName(army);
            switch (ContextEncoder.KindOf(feature))
            {
                case ContextEncoder.FeatureKind.Spot:
                {
                    int spot = feature / MapConstants.TowerTypeCount;
                    var tower = (TowerType)(feature % MapConstants.TowerTypeCount);
                    string where = ContextEncoder.SpotName(spot);
                    return helpsEnemy
                        ? "Your " + tower + " at " + where + " does little against our " + armyName + "."
                        : "Your " + tower + " at " + where + " punishes our " + armyName + ".";
                }
                case ContextEncoder.FeatureKind.Weapon:
                {
                    var weapon = (WeaponType)(feature - ContextEncoder.WeaponOffset);
                    if (helpsEnemy && army == ArmyType.AirRaid && weapon == WeaponType.Sword)
                        return "Your sword can't reach the sky.";
                    if (helpsEnemy && army == ArmyType.ShieldWall && weapon == WeaponType.Bow)
                        return "Your arrows bounce off our shields.";
                    return helpsEnemy
                        ? "Your " + weapon + " is weak against our " + armyName + "."
                        : "Your " + weapon + " cuts through our " + armyName + ".";
                }
                case ContextEncoder.FeatureKind.HeroLane:
                {
                    string lane = MapConstants.LaneNames[feature - ContextEncoder.HeroLaneOffset];
                    return helpsEnemy
                        ? "You keep guarding the " + lane + " lane. We know where you'll be."
                        : "Your presence in the " + lane + " lane slows us down.";
                }
                default:
                    return "";
            }
        }

        static string Percent(float value, float max) =>
            max <= 0 ? "0%" : Math.Round(100.0 * value / max) + "%";
    }
}
