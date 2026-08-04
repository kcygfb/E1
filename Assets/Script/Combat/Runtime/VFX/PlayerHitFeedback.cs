using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 玩家受击动效（闪红+震动+缩放弹回）。
    /// 监听引擎 DamageApplied 事件（TargetId=player），在玩家血量下降时触发。
    /// 挂在玩家立绘 Image 上。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class PlayerHitFeedback : MonoBehaviour
    {
        [Header("受击缩放")]
        [SerializeField] private float hitScalePunch = 0.85f;
        [SerializeField] private float hitScaleDuration = 0.2f;

        [Header("受击闪烁")]
        [SerializeField] private Color flashColor = new(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private float flashDuration = 0.2f;

        [Header("震动")]
        [SerializeField] private float shakeStrength = 25f;
        [SerializeField] private float shakeDuration = 0.25f;
        [SerializeField] private int shakeVibrato = 20;

        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        private RectTransform _rect;
        private Image _image;
        private Color _originColor;
        private Sequence _seq;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            if (_image != null)
                _originColor = _image.color;
        }

        private void Start()
        {
            if (battleController == null || !battleController.IsInitialized)
            {
                var root = GameObject.Find("BattleController");
                if (root != null)
                    battleController = root.GetComponent<BattleController>();
            }
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
            if (evt.Type != CombatEventType.DamageApplied) return;
            if (battleController == null || !battleController.IsInitialized) return;
            if (evt.TargetId != battleController.State.Player.Id) return;
            if (evt.Amount <= 0) return;

            PlayHit();
        }

        /// <summary>播放受击动效</summary>
        public void PlayHit()
        {
            _seq?.Kill();
            var currentScale = _rect.localScale;

            _seq = DOTween.Sequence().SetUpdate(UpdateType.Late);

            // 瞬间闪红
            if (_image != null)
                _image.color = flashColor;

            // 缩放弹回（用 DOTween.To 显式设值，避免 Animator 的 scale 曲线覆盖 DOScale 的 start value）
            var punchScale = currentScale * hitScalePunch;
            _seq.Join(DOTween.To(
                () => 0f,
                t => _rect.localScale = Vector3.LerpUnclamped(punchScale, currentScale, t),
                1f, hitScaleDuration).SetEase(Ease.OutBack));

            // 震动
            _seq.Join(_rect.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false)
                .SetEase(Ease.InOutQuad));

            // 闪红→恢复
            if (_image != null)
            {
                _seq.Join(DOTween.To(() => _image.color, c => _image.color = c, _originColor, flashDuration)
                    .SetEase(Ease.OutQuart));
            }
        }
    }
}
