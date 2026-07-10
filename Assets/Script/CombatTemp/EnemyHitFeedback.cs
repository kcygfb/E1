using UnityEngine;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 敌人受击动效：被打时向后退一下，然后归位。
    /// 挂到 EnemyPortrait（与 EnemyStats 同物体）上即可。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EnemyHitFeedback : MonoBehaviour
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

        private RectTransform _rect;
        private Vector2 _originPos;
        private EnemyStats _stats;
        private UnityEngine.UI.Image _image;
        private Color _originColor;
        private Sequence _seq;
        private int _lastHP;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _originPos = _rect.anchoredPosition;
            _stats = GetComponent<EnemyStats>();
            _image = GetComponent<UnityEngine.UI.Image>();
            if (_image != null)
                _originColor = _image.color;
        }

        private void OnEnable()
        {
            if (_stats == null) return;
            _stats.OnHPChanged.AddListener(OnHPChanged);
        }

        private void Start()
        {
            // 在所有 Awake 完成后记录初始血量，确保 _stats.CurrentHP 已初始化
            if (_stats != null)
                _lastHP = _stats.CurrentHP;
        }

        private void OnDisable()
        {
            if (_stats == null) return;
            _stats.OnHPChanged.RemoveListener(OnHPChanged);
        }

        private void OnHPChanged(int current, int max)
        {
            // 只有血量下降时才播受击动效
            if (current >= _lastHP)
            {
                _lastHP = current;
                return;
            }

            _lastHP = current;
            PlayHit();
        }

        private void PlayHit()
        {
            _seq?.Kill();

            _seq = DOTween.Sequence();

            // 向后退
            _seq.Append(_rect.DOAnchorPosX(_originPos.x + knockbackDistance, knockbackDuration)
                .SetEase(Ease.OutQuad));

            // 归位
            _seq.Append(_rect.DOAnchorPosX(_originPos.x, returnDuration)
                .SetEase(Ease.OutQuart));

            // 震动
            _seq.Join(_rect.DOShakeAnchorPos(shakeVibrato * 0.01f, shakeStrength, shakeVibrato, 90f, false, true, ShakeRandomnessMode.Full)
                .SetEase(Ease.InOutQuad));

            // 闪红
            if (_image != null)
            {
                _seq.Join(DOTween.To(() => _image.color, c => _image.color = c, hitColor, flashDuration * 0.5f)
                    .OnComplete(() =>
                    {
                        if (_image != null)
                            _image.DOColor(_originColor, flashDuration);
                    }));
            }
        }

        private void OnDestroy()
        {
            _seq?.Kill();
        }
    }
}
