using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 刀光/斩击特效：Image 缩放+旋转+颜色渐变+淡出，播完自动销毁。
    /// 发光用底层叠加 Image 实现，不用 Outline（Outline 会让颜色变脏）。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SlashVFX : MonoBehaviour
    {
        [Header("动画")]
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private float startScale = 0.3f;
        [SerializeField] private float endScale = 1.5f;

        [Header("方向")]
        [Tooltip("起始旋转角度（度），0=朝右，90=朝上")]
        [SerializeField] private float startAngle = 135f;
        [Tooltip("结束旋转角度（度）")]
        [SerializeField] private float endAngle = -45f;
        [SerializeField] private bool flipX = false;
        [SerializeField] private bool flipY = false;

        [Header("颜色渐变")]
        [SerializeField] private Color startColor = Color.white;
        [Tooltip("渐变结束颜色，alpha 会被忽略（统一淡出）")]
        [SerializeField] private Color endColor = new(1f, 0.2f, 0.2f, 1f);

        [Header("发光")]
        [Tooltip("发光层数，每层更大更透明，叠加出bloom效果")]
        [SerializeField] private int glowLayers = 3;
        [Tooltip("每层缩放递增倍数")]
        [SerializeField] private float glowScaleStep = 0.15f;
        [Tooltip("最内层发光透明度")]
        [SerializeField] private float glowAlpha = 0.5f;

        private Image _image;
        private Image[] _glowImages;
        private Sequence _seq;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        /// <param name="start">起始角度，0=右，90=上</param>
        /// <param name="end">结束角度</param>
        public void SetDirection(float start, float end)
        {
            startAngle = start;
            endAngle = end;
        }

        /// <param name="start">起始颜色</param>
        /// <param name="end">渐变目标颜色</param>
        public void SetColorGradient(Color start, Color end)
        {
            startColor = start;
            endColor = end;
        }

        public void SetFlip(bool x, bool y)
        {
            flipX = x;
            flipY = y;
        }

        public void Play()
        {
            // 创建多层发光 Image（每层更大更透明）
            if (_glowImages == null && _image.sprite != null && glowLayers > 0)
            {
                _glowImages = new Image[glowLayers];
                for (int i = 0; i < glowLayers; i++)
                {
                    var glowObj = new GameObject($"Glow{i}");
                    glowObj.transform.SetParent(transform, false);
                    var glowRt = glowObj.AddComponent<RectTransform>();
                    glowRt.anchorMin = Vector2.zero;
                    glowRt.anchorMax = Vector2.one;
                    glowRt.offsetMin = Vector2.zero;
                    glowRt.offsetMax = Vector2.zero;

                    var gi = glowObj.AddComponent<Image>();
                    gi.sprite = _image.sprite;
                    gi.raycastTarget = false;
                    gi.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
                    glowObj.transform.SetAsFirstSibling();
                    _glowImages[i] = gi;
                }
            }

            var flipScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
            transform.localScale = flipScale * startScale;
            transform.localRotation = Quaternion.Euler(0, 0, startAngle);

            // 主 Image 初始纯色
            var c0 = startColor;
            c0.a = 1f;
            _image.color = c0;

            _seq?.Kill();
            _seq = DOTween.Sequence();

            // 缩放
            _seq.Join(DOTween.To(() => transform.localScale,
                v => transform.localScale = v, flipScale * endScale, duration)
                .SetEase(Ease.OutQuart));

            // 旋转
            _seq.Join(transform.DOLocalRotate(new Vector3(0, 0, endAngle), duration)
                .SetEase(Ease.OutQuart));

            // 颜色渐变：白→红
            var c1 = endColor;
            c1.a = 1f;
            _seq.Join(DOTween.To(() => _image.color, c => _image.color = c, c1, duration)
                .SetEase(Ease.InQuad));

            // 最后 40% 淡出 alpha
            var fadeOut = c1;
            fadeOut.a = 0f;
            _seq.Append(DOTween.To(() => _image.color, c => _image.color = c, fadeOut, duration * 0.4f)
                .SetEase(Ease.InQuart));

            // 多层发光：每层比主图大，透明度递减
            if (_glowImages != null)
            {
                for (int i = 0; i < _glowImages.Length; i++)
                {
                    var layerScaleMul = 1f + glowScaleStep * (i + 1);
                    var layerAlpha = glowAlpha * (1f - (float)i / _glowImages.Length);

                    var glowStart = new Color(startColor.r, startColor.g, startColor.b, layerAlpha);
                    var glowEnd = new Color(endColor.r, endColor.g, endColor.b, 0f);

                    // 初始 scale
                    _glowImages[i].transform.localScale = Vector3.one;
                    var glowScaleStart = flipScale * (startScale * layerScaleMul);
                    var glowScaleEnd = flipScale * (endScale * layerScaleMul);

                    _seq.Join(DOTween.To(() => _glowImages[i].transform.localScale,
                        v => _glowImages[i].transform.localScale = v, glowScaleEnd, duration)
                        .From(glowScaleStart)
                        .SetEase(Ease.OutQuart));

                    _seq.Join(DOTween.To(() => _glowImages[i].color,
                        c => _glowImages[i].color = c, glowStart, duration * 0.3f)
                        .SetEase(Ease.OutQuad));
                    _seq.Join(DOTween.To(() => _glowImages[i].color,
                        c => _glowImages[i].color = c, glowEnd, duration * 0.7f)
                        .SetEase(Ease.InQuart));
                }
            }

            _seq.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            _seq?.Kill();
        }
    }
}
