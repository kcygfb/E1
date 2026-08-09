using System.Collections.Generic;
using KiKs.Audio;
using UnityEngine;

namespace KiKs.Combat
{
    [CreateAssetMenu(fileName = "BattleAudioBindings", menuName = "KiKs/Audio/Battle Audio Bindings", order = 20)]
    public sealed class BattleAudioBindings : ScriptableObject
    {
        [Header("Card movement")]
        [Tooltip("Played once for each successfully drawn card.")]
        public AudioCue cardDraw;
        [Tooltip("Played once when the discard pile is shuffled back into the draw pile.")]
        public AudioCue deckReshuffled;
        [Tooltip("Optional. Usually leave empty if discarding should be silent.")]
        public AudioCue cardDiscard;

        [Header("Successful card play by category")]
        [Tooltip("Used by heavy, bleed, flexible, hidden and legacy melee cards.")]
        public AudioCue meleeCardPlayed;
        public AudioCue rangedCardPlayed;
        public AudioCue magicCardPlayed;
        [Tooltip("Used by misc and legacy defense cards.")]
        public AudioCue defenseCardPlayed;
        [Tooltip("Used for enemy cards and any category without an assigned category cue.")]
        public AudioCue fallbackCardPlayed;

        [Header("Combat results")]
        [Tooltip("Damage received by the player. Enemy hit timing belongs on PlayerAttackFeedback.")]
        public AudioCue playerHit;
        [Tooltip("Damage received by an enemy.")]
        public AudioCue enemyHit;
        [Tooltip("Played once when an enemy is killed.")]
        public AudioCue enemyKilled;
        public AudioCue toughnessBroken;
        public AudioCue healing;
        public AudioCue statusApplied;

        [Header("Battle outcome")]
        public AudioCue victory;
        public AudioCue defeat;

        public AudioCue ResolveCardPlayed(string category)
        {
            AudioCue categoryCue;
            switch ((category ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "heavy":
                case "bleed":
                case "flexible":
                case "hidden":
                case "melee":
                    categoryCue = meleeCardPlayed;
                    break;
                case "ranged":
                case "guns":
                    categoryCue = rangedCardPlayed;
                    break;
                case "magic":
                    categoryCue = magicCardPlayed;
                    break;
                case "misc":
                case "defense":
                    categoryCue = defenseCardPlayed;
                    break;
                default:
                    categoryCue = null;
                    break;
            }

            return categoryCue != null ? categoryCue : fallbackCardPlayed;
        }

        public IEnumerable<AudioCue> EnumerateAssignedCues()
        {
            if (cardDraw != null) yield return cardDraw;
            if (deckReshuffled != null) yield return deckReshuffled;
            if (cardDiscard != null) yield return cardDiscard;
            if (meleeCardPlayed != null) yield return meleeCardPlayed;
            if (rangedCardPlayed != null) yield return rangedCardPlayed;
            if (magicCardPlayed != null) yield return magicCardPlayed;
            if (defenseCardPlayed != null) yield return defenseCardPlayed;
            if (fallbackCardPlayed != null) yield return fallbackCardPlayed;
            if (playerHit != null) yield return playerHit;
            if (enemyHit != null) yield return enemyHit;
            if (enemyKilled != null) yield return enemyKilled;
            if (toughnessBroken != null) yield return toughnessBroken;
            if (healing != null) yield return healing;
            if (statusApplied != null) yield return statusApplied;
            if (victory != null) yield return victory;
            if (defeat != null) yield return defeat;
        }
    }
}
