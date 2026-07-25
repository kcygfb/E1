using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
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
        private Image _glowImage;
        private CardSkew _glowSkew;
        private bool _glowDestroyed;

        private Image _flashImage;
        private Sequence _currentSeq;
        private Draggable _draggable;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _originPos = _rect.localPosition;
            _originScale = _rect.localScale;
            _draggable = GetComponent<Draggable>();

            var oldOutline = GetComponent<Outline>();
            if (oldOutline != null) Destroy(oldOutline);

            _skew = GetComponent<CardSkew>();
            if (_skew == null)
                _skew = gameObject.AddComponent<CardSkew>();

            // 高光边框：兄弟物体（渲染在卡牌后面），用 LateUpdate 同步位置
            var glowGo = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image), typeof(CardSkew));
            glowGo.transform.SetParent(_rect.parent, false);
            glowGo.transform.SetSiblingIndex(_rect.GetSiblingIndex());
            _glowRect = glowGo.GetComponent<RectTransform>();
            _glowRect.anchorMin = _rect.anchorMin;
            _glowRect.anchorMax = _rect.anchorMax;
            _glowRect.pivot = _rect.pivot;
            _glowRect.sizeDelta = _rect.sizeDelta + new Vector2(glowExpand * 2, glowExpand * 2);
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
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _currentSeq?.Kill();

            _currentSeq = DOTween.Sequence();
            _currentSeq.Join(_rect.DOScale(_originScale * hoverScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_rect.DOLocalMoveY(_originPos.y + hoverLiftY, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));

            if (!_glowDestroyed && _glowSkew != null)
            {
                _currentSeq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
                _currentSeq.Join(_glowImage.DOFade(1f, glowFadeDuration / speedMultiplier));
            }
        }

        // ── 离开 ──────────────────────────────────────────────

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _currentSeq?.Kill();

            _currentSeq = DOTween.Sequence();
            _currentSeq.Join(_rect.DOScale(_originScale, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(_rect.DOLocalMoveY(_originPos.y, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
            _currentSeq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));

            if (!_glowDestroyed && _glowSkew != null)
            {
                _currentSeq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, hoverDuration / speedMultiplier).SetEase(Ease.OutQuint));
                _currentSeq.Join(_glowImage.DOFade(0f, glowFadeDuration / speedMultiplier));
            }
        }

        // ── 点击 ──────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _currentSeq?.Kill();

            _rect.DOScale(_originScale * clickScale, clickDuration / speedMultiplier).SetEase(Ease.InQuad);

            DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, clickDuration / speedMultiplier).SetEase(Ease.OutQuint);
            if (!_glowDestroyed && _glowSkew != null)
                DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, clickDuration / speedMultiplier).SetEase(Ease.OutQuint);

            if (_flashImage != null)
            {
                _flashImage.color = flashColor;
                _flashImage.DOFade(0f, flashFadeDuration / speedMultiplier).SetEase(Ease.OutQuint);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            bool stillHovering = _rect.localPosition.y > _originPos.y + 1f;
            float targetScale = stillHovering ? hoverScale : 1f;
            _rect.DOScale(_originScale * targetScale, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);

            if (stillHovering)
            {
                DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);
                if (!_glowDestroyed && _glowSkew != null)
                    DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, releaseDuration / speedMultiplier).SetEase(Ease.OutQuint);
            }
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>Used by <c>CardView.SyncCardInteraction</c> after a card is moved to the hand.</summary>
        public void UpdateOrigin(Vector3 position, Vector3 scale)
        {
            _originPos = position;
            _originScale = scale;
        }

        // ── 清理 ──────────────────────────────────────────────

        public void DestroyGlow()
        {
            _currentSeq?.Kill(false);
            _currentSeq = null;
            if (_glowSkew != null) DOTween.Kill(_glowSkew, false);
            if (_glowImage != null) DOTween.Kill(_glowImage, false);
            if (_glowRect != null && _glowRect.gameObject != null)
                Destroy(_glowRect.gameObject);
            _glowRect = null;
            _glowSkew = null;
            _glowImage = null;
            _glowDestroyed = true;
        }

        private void LateUpdate()
        {
            if (_glowRect == null) return;
            _glowRect.position = _rect.position;
            _glowRect.rotation = _rect.rotation;
            _glowRect.localScale = _rect.localScale;
            _glowRect.sizeDelta = _rect.sizeDelta + new Vector2(glowExpand * 2, glowExpand * 2);
        }

        private void OnDestroy()
        {
            _currentSeq?.Kill(false);
            if (_glowSkew != null) DOTween.Kill(_glowSkew, false);
            if (_glowImage != null) DOTween.Kill(_glowImage, false);
            if (_flashImage != null) DOTween.Kill(_flashImage, false);
            if (_glowRect != null && _glowRect.gameObject != null)
                Destroy(_glowRect.gameObject);
        }
    }
}
