using System.Collections.Generic;
using KiKs.Audio;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Converts rule events into sound requests. It never changes combat state; every mapping is
    /// visible in the assigned BattleAudioBindings asset.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [AddComponentMenu("KiKs/Audio/Battle Audio Presenter")]
    public sealed class BattleAudioPresenter : MonoBehaviour
    {
        [Tooltip("May be left empty; the presenter finds the scene BattleController.")]
        [SerializeField] private BattleController battleController;
        [Tooltip("The single explicit registration page for battle event sounds.")]
        [SerializeField] private BattleAudioBindings bindings;

        private void Start()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();

            if (battleController == null)
            {
                Debug.LogWarning("[BattleAudioPresenter] No BattleController was found.", this);
                return;
            }

            if (bindings == null)
            {
                Debug.LogWarning(
                    "[BattleAudioPresenter] Battle Audio Bindings is empty. " +
                    "Create one through Create > KiKs > Audio > Battle Audio Bindings.", this);
                return;
            }

            foreach (var cue in bindings.EnumerateAssignedCues())
                AudioManager.Preload(cue);

            battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent == null || bindings == null) return;

            switch (combatEvent.Type)
            {
                case CombatEventType.CardDrawn:
                    AudioManager.TryPlay(bindings.cardDraw);
                    break;
                case CombatEventType.DeckReshuffled:
                    AudioManager.TryPlay(bindings.deckReshuffled);
                    break;
                case CombatEventType.CardDiscarded:
                    AudioManager.TryPlay(bindings.cardDiscard);
                    break;
                case CombatEventType.CardPlayed:
                    AudioManager.TryPlay(bindings.ResolveCardPlayed(
                        FindCardCategory(combatEvent.SourceId, combatEvent.CardInstanceId)));
                    break;
                case CombatEventType.DamageApplied:
                    if (combatEvent.Amount <= 0) break;
                    AudioManager.TryPlay(IsPlayer(combatEvent.TargetId)
                        ? bindings.playerHit
                        : bindings.enemyHit);
                    break;
                case CombatEventType.StatusTicked:
                    if (IsPlayer(combatEvent.TargetId) && combatEvent.Amount > 0)
                        AudioManager.TryPlay(bindings.playerHit);
                    break;
                case CombatEventType.CombatantDied:
                    if (!IsPlayer(combatEvent.TargetId))
                        AudioManager.TryPlay(bindings.enemyKilled);
                    break;
                case CombatEventType.ToughnessBroken:
                    AudioManager.TryPlay(bindings.toughnessBroken);
                    break;
                case CombatEventType.HealingApplied:
                    if (combatEvent.Amount > 0) AudioManager.TryPlay(bindings.healing);
                    break;
                case CombatEventType.StatusApplied:
                    AudioManager.TryPlay(bindings.statusApplied);
                    break;
                case CombatEventType.Victory:
                    AudioManager.TryPlay(bindings.victory);
                    break;
                case CombatEventType.Defeat:
                    AudioManager.TryPlay(bindings.defeat);
                    break;
            }
        }

        private bool IsPlayer(string combatantId)
        {
            return battleController != null && battleController.State != null &&
                   battleController.State.Player != null &&
                   battleController.State.Player.Id == combatantId;
        }

        private string FindCardCategory(string sourceId, string instanceId)
        {
            var state = battleController != null ? battleController.State : null;
            if (state == null || string.IsNullOrWhiteSpace(instanceId)) return string.Empty;

            var card = FindCard(state.GetDeck(sourceId), instanceId);
            if (card != null) return card.Spec.Category;

            var specialCard = state.GetEnemySpecialCard(sourceId);
            return specialCard != null && specialCard.InstanceId == instanceId
                ? specialCard.Spec.Category
                : string.Empty;
        }

        private static CardInstance FindCard(DeckState deck, string instanceId)
        {
            if (deck == null) return null;
            var card = FindCard(deck.Hand, instanceId);
            if (card != null) return card;
            card = FindCard(deck.DrawPile, instanceId);
            return card ?? FindCard(deck.DiscardPile, instanceId);
        }

        private static CardInstance FindCard(IReadOnlyList<CardInstance> cards, string instanceId)
        {
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i].InstanceId == instanceId) return cards[i];
            }
            return null;
        }
    }
}
