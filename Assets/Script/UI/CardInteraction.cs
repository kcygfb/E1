using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 卡牌悬浮、点击动效。依赖 DOTween。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CardInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("全局速度倍率")]
        [Range(0.5f, 3f)]
        [SerializeField] private float speedMultiplier = 1.5f;

        [Header("悬浮")]
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float hoverLiftY = 15f;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float hoverSkew = 10f;

        [Header("高光边框")]
        [SerializeField] private Color glowColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float glowExpand = 6f;
        [SerializeField] private float glowFadeDuration = 0.05f;
        [SerializeField] private float glowSkewRatio = 0.5f;

        [Header("点击")]
        [SerializeField] private float clickScale = 0.93f;
        [SerializeField] private float clickDuration = 0.05f;
        [SerializeField] private float releaseDuration = 0.12f;

        [Header("点击闪烁")]
        [SerializeField] private Color flashColor = new(1f, 0.9f, 0.5f, 1f);
        [SerializeField] private float flashFadeDuration = 0.4f;

        private RectTransform _rect;
        private Vector3 _originPos;
        private Vector3 _originScale;
        private CardSkew _skew;

        private RectTransform _glowRect;
        private Vector3 _glowOriginPos;
        private Vector3 _glowOriginScale;
        private Image _glowImage;
        private CardSkew _glowSkew;

        private Image _flashImage;
        private Sequence _currentSeq;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _originPos = _rect.localPosition;
            _originScale = _rect.localScale;

            // 清理旧版组件
            var oldOutline = GetComponent<Outline>();
            if (oldOutline != null) Destroy(oldOutline);

            // 卡牌切变
            _skew = GetComponent<CardSkew>();
            if (_skew == null)
                _skew = gameObject.AddComponent<CardSkew>();

            // 高光边框：作为兄弟物体（渲染在卡牌后面）
            var glowGo = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image), typeof(CardSkew));
            glowGo.transform.SetParent(_rect.parent, false);
            glowGo.transform.SetSiblingIndex(_rect.GetSiblingIndex());
            _glowRect = glowGo.GetComponent<RectTransform>();
            _glowRect.anchorMin = _rect.anchorMin;
            _glowRect.anchorMax = _rect.anchorMax;
            _glowRect.pivot = _rect.pivot;
            _glowRect.anchoredPosition = _rect.anchoredPosition;
            _glowRect.localRotation = _rect.localRotation;
            _glowRect.localScale = _rect.localScale;
            float expandX = glowExpand / Mathf.Abs(_originScale.x);
            float expandY = glowExpand / Mathf.Abs(_originScale.y);
            _glowRect.sizeDelta = _rect.sizeDelta + new Vector2(expandX * 2, expandY * 2);
            _glowOriginPos = _glowRect.localPosition;
            _glowOriginScale = _glowRect.localScale;
            _glowImage = glowGo.GetComponent<Image>();
            _glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            _glowImage.raycastTarget = false;
            _glowSkew = glowGo.GetComponent<CardSkew>();

            // 闪烁层（子物体，渲染在卡牌上方）
            var flashGo = new GameObject("FlashLayer", typeof(RectTransform), typeof(Image));
            flashGo.transform.SetParent(transform, false);
            flashGo.transform.SetAsLastSibling();
            var flashRect = flashGo.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            _flashImage = flashGo.GetComponent<Image>();
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;
        }

        // ── 悬浮 ──────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            _currentSeq?.Kill();

            _currentSeq = DOTween.Sequence();
            _currentSeq.Join(_rect.DOScale(_originScale * hoverScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_rect.DOLocalMoveY(_originPos.y + hoverLiftY, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));

            _currentSeq.Join(_glowRect.DOScale(_glowOriginScale * hoverScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_glowRect.DOLocalMoveY(_glowOriginPos.y + hoverLiftY, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_glowImage.DOFade(1f, glowFadeDuration / speedMultiplier));
        }

        // ── 离开 ──────────────────────────────────────────────

        public void OnPointerExit(PointerEventData eventData)
        {
            _currentSeq?.Kill();

            _currentSeq = DOTween.Sequence();
            _currentSeq.Join(_rect.DOScale(_originScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_rect.DOLocalMoveY(_originPos.y, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));

            _currentSeq.Join(_glowRect.DOScale(_glowOriginScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_glowRect.DOLocalMoveY(_glowOriginPos.y, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_glowImage.DOFade(0f, glowFadeDuration / speedMultiplier));
        }

        // ── 点击 ──────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            _currentSeq?.Kill();

            _rect.DOScale(_originScale * clickScale, clickDuration / speedMultiplier).SetEase(Ease.InQuad);
            _glowRect.DOScale(_glowOriginScale * clickScale, clickDuration / speedMultiplier).SetEase(Ease.InQuad);

            // 形变归零
            DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, clickDuration / speedMultiplier).SetEase(Ease.OutQuint);
            DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, clickDuration / speedMultiplier).SetEase(Ease.OutQuint);

            _flashImage.color = flashColor;
            _flashImage.DOFade(0f, flashFadeDuration / speedMultiplier).SetEase(Ease.OutQuint);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool stillHovering = _rect.localPosition.y > _originPos.y + 1f;
            float targetScale = stillHovering ? hoverScale : 1f;
            _rect.DOScale(_originScale * targetScale, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);
            _glowRect.DOScale(_glowOriginScale * targetScale, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);

            // 如果还在悬浮，恢复形变
            if (stillHovering)
            {
                DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);
                DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);
            }
        }

        private void OnDestroy()
        {
            _currentSeq?.Kill();
            if (_glowRect != null && _glowRect.gameObject != null)
                Destroy(_glowRect.gameObject);
        }
    }
}
