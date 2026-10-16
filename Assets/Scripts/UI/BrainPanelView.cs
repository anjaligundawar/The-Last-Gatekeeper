using System;
using Gatekeeper.AI;
using TMPro;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Live view of the War Council's model (Tab). Works with any IExplainablePolicy:
    /// one row per plan, plus a heat tint on each lane showing where the Council expects to do best.
    /// </summary>
    public class BrainPanelView : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text infoText;
        [Tooltip("One row per plan, 16 in arm order.")]
        [SerializeField] BrainRow[] rows = new BrainRow[PlanCatalog.Count];
        [SerializeField, Range(0f, 1f)] float maxHeatAlpha = 0.45f;

        public void Refresh(GameManager gm, bool visible)
        {
            var scores = visible ? Scores(gm) : null;
            UpdateLaneHeat(gm, scores);
            if (!visible) return;

            titleText.text = "AI brain: " + gm.Council.Policy.Name;
            bool hasScores = scores != null;
            foreach (var row in rows) row.gameObject.SetActive(hasScores);
            if (!hasScores)
            {
                infoText.text = "This policy can't explain itself yet. Implement LinUCBPolicy (an IExplainablePolicy) " +
                                "and set the policy to LinUCB in GameConfig to see its predictions here.";
                return;
            }

            var explainable = (IExplainablePolicy)gm.Council.Policy;
            infoText.text = "Learned from " + explainable.UpdatesSeen + " waves. Solid = expected reward, light = exploration bonus.";

            float max = 0.0001f;
            int best = 0;
            for (int a = 0; a < scores.Length; a++)
            {
                max = Mathf.Max(max, scores[a].Total);
                if (scores[a].Total > scores[best].Total) best = a;
            }
            int chosen = gm.Phase == GamePhase.Wave ? gm.CurrentArm : best;
            for (int a = 0; a < rows.Length && a < scores.Length; a++)
                rows[a].Set(PlanCatalog.Describe(a), scores[a].Mean, scores[a].Bonus, max, a == chosen);
        }

        void UpdateLaneHeat(GameManager gm, ArmScore[] scores)
        {
            var heat = new float[MapConstants.LaneCount];
            if (scores != null)
            {
                float max = 0.0001f;
                for (int a = 0; a < scores.Length; a++)
                {
                    var plan = PlanCatalog.FromArm(a);
                    if (plan.Focus == LaneFocus.Even) continue;
                    heat[(int)plan.Focus] = Mathf.Max(heat[(int)plan.Focus], scores[a].Mean);
                    max = Mathf.Max(max, scores[a].Mean);
                }
                for (int i = 0; i < heat.Length; i++) heat[i] = Mathf.Clamp01(heat[i] / max);
            }

            foreach (var lane in gm.Lanes)
            {
                if (lane.HeatOverlay == null) continue;
                var c = lane.HeatOverlay.color;
                c.a = heat[lane.Index] * maxHeatAlpha;
                lane.HeatOverlay.color = c;
            }
        }

        /// <summary>Scores for the current layout, or null if the policy can't explain itself (or isn't implemented).</summary>
        static ArmScore[] Scores(GameManager gm)
        {
            if (!(gm.Council.Policy is IExplainablePolicy explainable)) return null;
            var context = gm.Phase == GamePhase.Wave && gm.CurrentContext != null ? gm.CurrentContext : PreviewContext(gm);
            try { return explainable.ScoreAll(context); }
            catch (NotImplementedException) { return null; }
        }

        /// <summary>While building, show what the Council would think of the layout so far.</summary>
        static float[] PreviewContext(GameManager gm)
        {
            var snapshot = new DefenseSnapshot { Weapon = gm.Hero.Combat.Weapon };
            gm.Build.WriteSnapshot(snapshot);
            return ContextEncoder.Encode(snapshot);
        }
    }
}
