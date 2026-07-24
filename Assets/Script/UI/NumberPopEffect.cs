using UnityEngine;
using DG.Tweening;
using TMPro;

namespace KiKs.UI
{
    /// <summary>
    /// 通用数值变化动效：数字弹跳缩放 + 颜色闪烁。
    /// 挂在任意 TMP_Text 上，外部通过 SetValue 设置新值即可触发动画。
    /// 支持整数递增/递减和直接跳变两种模式。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class NumberPopEffect : MonoBehaviour
    {
        [Header("弹跳")]
        [SerializeField] private bool enableScalePunch = true;
        [SerializeField] private float punchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.25f;

        [Header("颜色闪烁")]
        [SerializeField] private Color popColor = new(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float colorDuration = 0.3f;

        [Header("数字递变")]
        [SerializeField] private bool animateCount = true;
        [SerializeField] private float countDuration = 0.4f;

        private TMP_Text _text;
        private Color _originColor;
        private Vector3 _originScale;
        private int _currentValue;
        private Sequence _seq;
        private bool _initialized;
        private bool _firstSet = true;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _originColor = _text.color;
            _originScale = transform.localScale;
            _initialized = true;
        }

        private void OnDestroy()
        {
            _seq?.Kill();
        }

        /// <summary>设置数值并播放变化动效</summary>
        public void SetValue(int newValue)
        {
            if (!_initialized) return;

            // 首次设置：直接赋值，不播放动效
            if (_firstSet)
            {
                _firstSet = false;
                _currentValue = newValue;
                _text.text = newValue.ToString();
                return;
            }

            // 数值没变：不播放动效
            if (_currentValue == newValue) return;

            _seq?.Kill();
            transform.localScale = _originScale;

            var oldValue = _currentValue;

            _seq = DOTween.Sequence();

            // 弹跳缩放
            if (enableScalePunch)
            {
                transform.localScale = _originScale * punchScale;
                _seq.Join(transform.DOScale(_originScale, punchDuration).SetEase(Ease.OutBack));
            }

            // 颜色闪烁
            _text.color = popColor;
            _seq.Join(DOTween.To(() => _text.color, c => _text.color = c, _originColor, colorDuration)
                .SetEase(Ease.OutQuart));

            // 数字递变
            if (animateCount && oldValue != newValue)
            {
                _seq.Join(DOTween.To(() => _currentValue, v =>
                {
                    _currentValue = Mathf.RoundToInt(v);
                    _text.text = _currentValue.ToString();
                }, newValue, countDuration).SetEase(Ease.OutQuart));
            }
            else
            {
                _currentValue = newValue;
                _text.text = newValue.ToString();
            }

            _seq.OnComplete(() =>
            {
                _currentValue = newValue;
                _text.text = newValue.ToString();
                _text.color = _originColor;
            });
        }

        /// <summary>直接设置数值，不播放动效</summary>
        public void SetValueSilent(int value)
        {
            _currentValue = value;
            if (_text != null)
                _text.text = value.ToString();
        }
    }
}
