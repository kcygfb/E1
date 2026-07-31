using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>Battle BGM controller: plays AudioSource on battle start, stops on battle end.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class BattleBGMController : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (battleController == null)
                battleController = GetComponentInParent<BattleController>();
        }

        private void OnEnable()
        {
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDisable()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            if (evt.Type == CombatEventType.BattleStarted)
            {
                if (_audioSource != null && !_audioSource.isPlaying)
                    _audioSource.Play();
            }
            else if (evt.Type == CombatEventType.Victory || evt.Type == CombatEventType.Defeat)
            {
                if (_audioSource != null && _audioSource.isPlaying)
                    _audioSource.Stop();
            }
        }
    }
}