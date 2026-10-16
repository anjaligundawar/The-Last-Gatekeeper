using UnityEngine;

namespace Gatekeeper
{
    public enum EnemyStateId
    {
        March,
        Chase,
        Attack,
        AttackGate,
    }

    /// <summary>
    /// One state of the enemy state machine. States are stateless singletons;
    /// per-enemy data (timers, waypoint index) lives on the Enemy.
    ///
    ///   March --(hero within DetectRange)--> Chase --(hero within AttackRange)--> Attack
    ///     ^                                    |                                     |
    ///     +-------(hero beyond LoseRange)------+<----(hero steps out of reach)-------+
    ///   March --(reached the gate)--> AttackGate
    /// Flyers never chase the hero.
    /// </summary>
    public interface IEnemyState
    {
        EnemyStateId Id { get; }
        void Enter(Enemy enemy);
        void Tick(Enemy enemy, float dt);
    }

    public static class EnemyStates
    {
        public static readonly IEnemyState March = new MarchState();
        public static readonly IEnemyState Chase = new ChaseState();
        public static readonly IEnemyState Attack = new AttackState();
        public static readonly IEnemyState AttackGate = new AttackGateState();

        public static IEnemyState Get(EnemyStateId id)
        {
            switch (id)
            {
                case EnemyStateId.Chase: return Chase;
                case EnemyStateId.Attack: return Attack;
                case EnemyStateId.AttackGate: return AttackGate;
                default: return March;
            }
        }
    }

    sealed class MarchState : IEnemyState
    {
        public EnemyStateId Id => EnemyStateId.March;
        public void Enter(Enemy enemy) { }

        public void Tick(Enemy enemy, float dt)
        {
            if (enemy.CanSeeHero(enemy.Data.detectRange))
            {
                enemy.ChangeState(EnemyStateId.Chase);
                return;
            }

            if (enemy.DistanceToGate() <= enemy.Gate.Reach)
            {
                enemy.ChangeState(EnemyStateId.AttackGate);
                return;
            }

            var lane = enemy.Lane;
            if (enemy.WaypointIndex >= lane.Count)
            {
                enemy.MoveTowards(enemy.Gate.AttackPoint, dt);
                return;
            }

            var target = lane.GetPoint(enemy.WaypointIndex);
            enemy.MoveTowards(target, dt);
            if (Vector2.Distance(enemy.Position, target) < 0.05f) enemy.WaypointIndex++;
        }
    }

    sealed class ChaseState : IEnemyState
    {
        public EnemyStateId Id => EnemyStateId.Chase;
        public void Enter(Enemy enemy) { }

        public void Tick(Enemy enemy, float dt)
        {
            if (!enemy.CanSeeHero(enemy.Data.loseRange))
            {
                enemy.ChangeState(EnemyStateId.March);
                return;
            }

            if (enemy.DistanceToHero() <= enemy.Data.attackRange)
            {
                enemy.ChangeState(EnemyStateId.Attack);
                return;
            }

            enemy.MoveTowards(enemy.Hero.Position, dt);
        }
    }

    sealed class AttackState : IEnemyState
    {
        public EnemyStateId Id => EnemyStateId.Attack;
        public void Enter(Enemy enemy) => enemy.AttackTimer = enemy.Data.attackInterval * 0.5f;

        public void Tick(Enemy enemy, float dt)
        {
            if (!enemy.CanSeeHero(enemy.Data.loseRange))
            {
                enemy.ChangeState(EnemyStateId.March);
                return;
            }

            if (enemy.DistanceToHero() > enemy.Data.attackRange * 1.3f)
            {
                enemy.ChangeState(EnemyStateId.Chase);
                return;
            }

            enemy.AttackTimer -= dt;
            if (enemy.AttackTimer <= 0f)
            {
                enemy.AttackTimer = enemy.Data.attackInterval;
                enemy.Hero.TakeDamage(enemy.Data.damage);
                enemy.Lunge();
            }
        }
    }

    sealed class AttackGateState : IEnemyState
    {
        public EnemyStateId Id => EnemyStateId.AttackGate;

        public void Enter(Enemy enemy)
        {
            enemy.MarkReachedGate();
            enemy.AttackTimer = 0f;
        }

        public void Tick(Enemy enemy, float dt)
        {
            enemy.AttackTimer -= dt;
            if (enemy.AttackTimer <= 0f)
            {
                enemy.AttackTimer = enemy.Data.attackInterval;
                enemy.Gate.Health.Damage(enemy.Data.damage);
                enemy.Lunge();
            }
        }
    }
}
