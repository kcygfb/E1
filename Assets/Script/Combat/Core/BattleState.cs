using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    public sealed class PendingExecutionState
    {
        public string TargetId { get; }
        public string SourceCardInstanceId { get; }

        public PendingExecutionState(string targetId, string sourceCardInstanceId)
        {
            TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
            SourceCardInstanceId = sourceCardInstanceId;
        }
    }

    /// <summary>Complete mutable state for one battle. Only CombatEngine advances the rules.</summary>
    public sealed class BattleState
    {
        private readonly List<CombatantState> _enemies;

        public CombatRules Rules { get; }
        public CombatantState Player { get; }
        public IReadOnlyList<CombatantState> Enemies { get; }
        public DeckState Deck { get; }
        public ManaState Mana { get; }
        public CombatPhase Phase { get; internal set; }
        public BattleOutcome Outcome { get; internal set; }
        public int TurnNumber { get; internal set; }
        public PendingExecutionState PendingExecution { get; internal set; }
        public bool IsCurrentEnemyTurnSkipped { get; internal set; }

        public BattleState(
            CombatRules rules,
            CombatantState player,
            IEnumerable<CombatantState> enemies,
            DeckState deck)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));

            if (player.Side != CombatantSide.Player)
                throw new ArgumentException("The battle player must use the Player side.", nameof(player));

            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            _enemies = new List<CombatantState>(enemies);
            if (_enemies.Count == 0) throw new ArgumentException("A battle needs at least one enemy.", nameof(enemies));

            var ids = new HashSet<string> { player.Id };
            foreach (var enemy in _enemies)
            {
                if (enemy == null) throw new ArgumentException("Enemy list contains null.", nameof(enemies));
                if (enemy.Side != CombatantSide.Enemy)
                    throw new ArgumentException("Every enemy must use the Enemy side.", nameof(enemies));
                if (!ids.Add(enemy.Id))
                    throw new ArgumentException("Duplicate combatant id: " + enemy.Id, nameof(enemies));
            }

            Enemies = new ReadOnlyCollection<CombatantState>(_enemies);
            Mana = new ManaState(rules.StartingMana, rules.MaximumMana);
            Phase = CombatPhase.NotStarted;
            Outcome = BattleOutcome.None;
        }

        public CombatantState FindEnemy(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return null;
            return _enemies.Find(enemy => enemy.Id == enemyId);
        }

        public CombatantState FindFirstLivingEnemy()
        {
            return _enemies.Find(enemy => !enemy.IsDead);
        }
    }
}
