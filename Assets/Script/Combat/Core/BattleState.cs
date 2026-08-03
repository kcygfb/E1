using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{

    /// <summary>Complete mutable state for one battle. Only CombatEngine advances the rules.</summary>
    public sealed class BattleState
    {
        private readonly List<CombatantState> _enemies;
        private readonly Dictionary<string, DeckState> _enemyDecks = new();
        private readonly Dictionary<string, int> _enemyBaseActionPoints = new();
        private readonly Dictionary<string, CardInstance> _enemySpecialCards = new();

        public CombatRules Rules { get; }
        public CombatantState Player { get; }
        public IReadOnlyList<CombatantState> Enemies { get; }
        public DeckState Deck { get; }
        public ManaState Mana { get; }
        public CombatPhase Phase { get; internal set; }
        public BattleOutcome Outcome { get; internal set; }
        public int TurnNumber { get; internal set; }
        public bool IsCurrentEnemyTurnSkipped { get; internal set; }
        public IReadOnlyDictionary<string, DeckState> EnemyDecks => _enemyDecks;
        public IReadOnlyDictionary<string, int> EnemyBaseActionPoints => _enemyBaseActionPoints;

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

        public CombatantState FindCombatant(string combatantId)
        {
            if (string.IsNullOrWhiteSpace(combatantId)) return null;
            if (Player.Id == combatantId) return Player;
            return FindEnemy(combatantId);
        }

        public CombatantState FindFirstLivingOpponent(CombatantState source)
        {
            if (source == null) return null;
            return source.Side == CombatantSide.Player
                ? FindFirstLivingEnemy()
                : !Player.IsDead ? Player : null;
        }

        public DeckState GetDeck(string combatantId)
        {
            if (string.IsNullOrWhiteSpace(combatantId)) return null;
            return Player.Id == combatantId ? Deck : GetEnemyDeck(combatantId);
        }

        public DeckState GetEnemyDeck(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return null;
            return _enemyDecks.TryGetValue(enemyId, out var deck) ? deck : null;
        }

        public CardInstance GetEnemySpecialCard(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return null;
            return _enemySpecialCards.TryGetValue(enemyId, out var card) ? card : null;
        }

        internal void RegisterEnemySpecialCard(string enemyId, CardInstance card)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || card == null) return;
            _enemySpecialCards[enemyId] = card;
        }

        public void RegisterCombatantDeck(string combatantId, DeckState deck)
        {
            if (Phase != CombatPhase.NotStarted)
                throw new InvalidOperationException("Combatant decks can only be registered before battle start.");
            if (string.IsNullOrWhiteSpace(combatantId))
                throw new ArgumentException("Combatant id is required.", nameof(combatantId));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (combatantId == Player.Id)
                throw new ArgumentException("The player deck is supplied by the BattleState constructor.",
                    nameof(combatantId));
            if (FindEnemy(combatantId) == null)
                throw new ArgumentException("Unknown combatant id: " + combatantId, nameof(combatantId));

            _enemyDecks[combatantId] = deck;
        }

        internal void RegisterEnemyDeck(string enemyId, DeckState deck)
        {
            RegisterCombatantDeck(enemyId, deck);
        }

        internal void RegisterEnemyBaseActionPoints(string enemyId, int baseAP)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return;
            _enemyBaseActionPoints[enemyId] = baseAP;
        }

        public int GetEnemyBaseActionPoints(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return 0;
            return _enemyBaseActionPoints.TryGetValue(enemyId, out var ap) ? ap : 0;
        }
    }
}
