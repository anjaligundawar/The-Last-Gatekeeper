using System.Collections.Generic;
using UnityEngine;

namespace Gatekeeper
{
    /// <summary>Spawns a wave's enemies over time and counts how many reached the gate.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [Tooltip("Indexed by EnemyType: Grunt, Runner, Shield, Flyer.")]
        [SerializeField] Enemy[] enemyPrefabs = new Enemy[4];
        [Tooltip("Indexed by lane: Left, Middle, Right.")]
        [SerializeField] LanePath[] lanes = new LanePath[3];
        [SerializeField] Hero hero;
        [SerializeField] Gate gate;
        [SerializeField] Transform enemyParent;

        public bool IsComplete => started && queue.Count == 0 && EnemyRegistry.Count == 0;
        public int Spawned { get; private set; }
        public int ReachedGate { get; private set; }
        public LanePath[] Lanes => lanes;

        readonly Queue<SpawnOrder> queue = new Queue<SpawnOrder>();
        float hpScale;
        float timer;
        bool started;

        public int CostOf(EnemyType type) => enemyPrefabs[(int)type].Data.cost;

        void OnEnable() => Enemy.Removed += OnEnemyRemoved;
        void OnDisable() => Enemy.Removed -= OnEnemyRemoved;

        public void Begin(List<SpawnOrder> orders, float enemyHpScale)
        {
            queue.Clear();
            foreach (var order in orders) queue.Enqueue(order);
            hpScale = enemyHpScale;
            Spawned = 0;
            ReachedGate = 0;
            timer = 0f;
            started = true;
        }

        void Update()
        {
            if (!started || queue.Count == 0) return;
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Wave) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.spawnInterval;
            Spawn(queue.Dequeue());
        }

        void Spawn(SpawnOrder order)
        {
            var lane = lanes[order.Lane];
            var enemy = Instantiate(enemyPrefabs[(int)order.Type], lane.Spawn, Quaternion.identity, enemyParent);
            enemy.Setup(lane, hero, gate, hpScale);
            Spawned++;
        }

        void OnEnemyRemoved(Enemy enemy)
        {
            if (!started) return;
            if (enemy.ReachedGate) ReachedGate++;
        }

        /// <summary>Ends the wave and clears any enemies still alive.</summary>
        public void Finish()
        {
            queue.Clear();
            foreach (var enemy in new List<Enemy>(EnemyRegistry.Alive)) enemy.Despawn();
            started = false;
        }
    }
}
