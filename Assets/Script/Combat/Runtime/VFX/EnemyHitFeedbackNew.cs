using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 敌人受击动效（击退+闪红+震动）。
    /// 由 PlayerAttackFeedback 在攻击命中时调用 PlayHit()，不再自行监听引擎事件。
    /// 挂在敌人立绘 Image 上。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class EnemyHitFeedbackNew : MonoBehaviour
    {
        [Header("受击位移")]
        [SerializeField] private float knockbackDistance = 60f;
        [SerializeField] private float knockbackDuration = 0.06f;
        [SerializeField] private float returnDuration = 0.3f;

        [Header("受击缩放（Squash & Stretch）")]
        [SerializeField] private float hitScalePunch = 1.3f;
        [SerializeField] private float hitScaleDuration = 0.18f;

        [Header("受击闪烁")]
        [Tooltip("先闪白再闪红，制造冲击感")]
        [SerializeField] private Color flashWhite = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color hitColor = new(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private float flashDuration = 0.15f;

        [Header("震动")]
        [SerializeField] private float shakeStrength = 20f;
        [SerializeField] private int shakeVibrato = 15;

        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        private RectTransform _rect;
        private Image _image;
        private Color _originColor;
        private Vector2 _originPos;
        private Vector3 _originScale;
        private Sequence _seq;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _originPos = _rect.anchoredPosition;
            _originScale = _rect.localScale;
            if (_image != null)
                _originColor = _image.color;
        }

        private void OnDestroy()
        {
            _seq?.Kill();
        }

        /// <summary>播放受击动效，由 PlayerAttackFeedback 在命中瞬间调用</summary>
        public void PlayHit()
        {
            _seq?.Kill();
            _rect.anchoredPosition = _originPos;
            _rect.localScale = _originScale;

            _seq = DOTween.Sequence();

            // 瞬间闪白
            if (_image != null)
                _image.color = flashWhite;

            // 瞬间放大（Squash）
            _rect.localScale = _originScale * hitScalePunch;

            // 击退
            _seq.Append(_rect.DOLocalMoveX(_originPos.x + knockbackDistance, knockbackDuration)
                .SetEase(Ease.OutQuad));

            // 震动
            _seq.Join(_rect.DOShakePosition(shakeVibrato * 0.01f, shakeStrength, shakeVibrato, 90f, false)
                .SetEase(Ease.InOutQuad));

            // 缩放弹回（Stretch）
            _seq.Join(_rect.DOScale(_originScale, hitScaleDuration).SetEase(Ease.OutBack));

            // 闪白→闪红→恢复
            if (_image != null)
            {
                _seq.Join(DOTween.To(() => _image.color, c => _image.color = c, hitColor, flashDuration * 0.3f)
                    .OnComplete(() =>
                    {
                        if (_image != null)
                            DOTween.To(() => _image.color, c => _image.color = c, _originColor, flashDuration);
                    }));
            }

            // 返回原位
            _seq.Append(_rect.DOLocalMoveX(_originPos.x, returnDuration)
                .SetEase(Ease.OutQuart));
        }
    }
}
