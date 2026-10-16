using System;
using System.Globalization;
using System.IO;
using System.Text;
using Gatekeeper.AI;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>
    /// Appends one CSV row per wave to Application.persistentDataPath/telemetry.csv.
    /// This is the dataset for evaluation plots and for the optional neural-network stretch goal.
    /// </summary>
    public class TelemetryLogger
    {
        public string FilePath { get; }
        public string RunId { get; }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public TelemetryLogger(string fileName)
        {
            FilePath = Path.Combine(Application.persistentDataPath, fileName);
            RunId = DateTime.UtcNow.ToString("yyyyMMddHHmmss", Inv) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        public void LogWave(int wave, string policy, bool randomPlan, int arm, float reward, WaveOutcome outcome, float[] context, string botPersona)
        {
            try
            {
                bool newFile = !File.Exists(FilePath);
                using (var writer = new StreamWriter(FilePath, append: true))
                {
                    if (newFile) writer.WriteLine(Header(context.Length));
                    var plan = PlanCatalog.FromArm(arm);
                    var sb = new StringBuilder();
                    sb.Append(RunId).Append(',').Append(wave).Append(',').Append(policy).Append(',')
                      .Append(randomPlan ? 1 : 0).Append(',').Append(botPersona ?? "human").Append(',')
                      .Append(arm).Append(',').Append(plan.Army).Append(',').Append(plan.Focus).Append(',')
                      .Append(F(reward)).Append(',').Append(F(outcome.GateDamage)).Append(',').Append(F(outcome.HeroDamage)).Append(',')
                      .Append(F(outcome.AvgProgress)).Append(',').Append(outcome.Spawned).Append(',')
                      .Append(outcome.Kills).Append(',').Append(outcome.ReachedGate);
                    foreach (var v in context) sb.Append(',').Append(F(v));
                    writer.WriteLine(sb.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Telemetry] Could not write " + FilePath + ": " + e.Message);
            }
        }

        static string Header(int dims)
        {
            var sb = new StringBuilder("run_id,wave,policy,random_plan,persona,arm,army,focus,reward,gate_dmg,hero_dmg,avg_progress,spawned,kills,reached_gate");
            for (int i = 0; i < dims; i++) sb.Append(",x").Append(i);
            return sb.ToString();
        }

        static string F(float v) => v.ToString("0.####", Inv);
    }
}
