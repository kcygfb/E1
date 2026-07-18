using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 监听引擎 CombatEvent，驱动敌人受击动效（击退+闪红+震动）。
    /// 挂在敌人立绘 Image 上，不需要 EnemyStats。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EnemyHitFeedbackNew : MonoBehaviour
    {
        [Header("受击位移")]
        [SerializeField] private float knockbackDistance = 30f;
        [SerializeField] private float knockbackDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.25f;

        [Header("受击闪烁")]
        [SerializeField] private Color hitColor = new(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private float flashDuration = 0.12f;

        [Header("震动")]
        [SerializeField] private float shakeStrength = 8f;
        [SerializeField] private int shakeVibrato = 10;

        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        private RectTransform _rect;
        private Image _image;
        private Color _originColor;
        private Vector2 _originPos;
        private Sequence _seq;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _originPos = _rect.anchoredPosition;
            if (_image != null)
                _originColor = _image.color;
        }

        private void Start()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
            _seq?.Kill();
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            if (evt.Type == CombatEventType.DamageApplied && !string.IsNullOrEmpty(evt.TargetId))
            {
                // 检查目标是不是敌人
                if (battleController == null || !battleController.IsInitialized) return;
                var enemy = battleController.State.FindEnemy(evt.TargetId);
                if (enemy != null && enemy.Side == CombatantSide.Enemy)
                    PlayHit();
            }
        }

        private void PlayHit()
        {
            _seq?.Kill();
            _rect.anchoredPosition = _originPos;

            _seq = DOTween.Sequence();

            _seq.Append(_rect.DOLocalMoveX(_originPos.x + knockbackDistance, knockbackDuration)
                .SetEase(Ease.OutQuad));

            _seq.Append(_rect.DOLocalMoveX(_originPos.x, returnDuration)
                .SetEase(Ease.OutQuart));

            _seq.Join(_rect.DOShakePosition(shakeVibrato * 0.01f, shakeStrength, shakeVibrato, 90f, false)
                .SetEase(Ease.InOutQuad));

            if (_image != null)
            {
                _seq.Join(DOTween.To(() => _image.color, c => _image.color = c, hitColor, flashDuration * 0.5f)
                    .OnComplete(() =>
                    {
                        if (_image != null)
                            DOTween.To(() => _image.color, c => _image.color = c, _originColor, flashDuration);
                    }));
            }
        }
    }
}
