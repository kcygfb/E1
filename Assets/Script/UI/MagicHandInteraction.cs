using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 魔手交互动效：悬浮缩放/上浮/倾斜 + 高光（同sprite轮廓） + 点击反馈。
    /// 独立于 CardInteraction，专用于 Magichand。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class MagicHandInteraction : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("悬浮")]
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float hoverLiftY = 15f;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float hoverSkew = 10f;

        [Header("高光")]
        [SerializeField] private Color glowColor = new(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float glowExpand = 6f;
        [SerializeField] private float glowFadeDuration = 0.08f;
        [SerializeField] private float glowSkewRatio = 0.5f;

        [Header("点击")]
        [SerializeField] private float clickScale = 0.93f;
        [SerializeField] private float clickDuration = 0.05f;
        [SerializeField] private float releaseDuration = 0.12f;

        private RectTransform _rect;
        private Vector3 _originPos;
        private Vector3 _originScale;
        private Draggable _draggable;

        private RectTransform _glowRect;
        private Image _glowImage;
        private CardSkew _glowSkew;
        private CardSkew _skew;

        private Sequence _seq;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _originPos = _rect.localPosition;
            _originScale = _rect.localScale;
            _draggable = GetComponent<Draggable>();

            // 给魔手自己加 CardSkew
            _skew = GetComponent<CardSkew>();
            if (_skew == null)
                _skew = gameObject.AddComponent<CardSkew>();

            CreateGlow();
        }

        private void CreateGlow()
        {
            var mainImage = GetComponent<Image>();

            // 高光作为兄弟节点，SetAsFirstSibling 渲染在魔手下面
            var glowGo = new GameObject("GlowBorder");
            glowGo.transform.SetParent(_rect.parent, false);
            glowGo.transform.SetAsFirstSibling();

            _glowRect = glowGo.AddComponent<RectTransform>();
            _glowRect.anchorMin = _rect.anchorMin;
            _glowRect.anchorMax = _rect.anchorMax;
            _glowRect.pivot = _rect.pivot;

            // 用魔手的同一张 sprite，这样高光轮廓跟着 PNG 形状走
            _glowImage = glowGo.AddComponent<Image>();
            if (mainImage != null && mainImage.sprite != null)
            {
                _glowImage.sprite = mainImage.sprite;
                _glowImage.type = mainImage.type;
                _glowImage.preserveAspect = mainImage.preserveAspect;
            }
            _glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            _glowImage.raycastTarget = false;

            // 给高光也加 CardSkew，和卡牌一样
            _glowSkew = glowGo.AddComponent<CardSkew>();

            SyncGlow();
        }

        private void SyncGlow()
        {
            if (_glowRect == null) return;
            _glowRect.position = _rect.position;
            _glowRect.rotation = _rect.rotation;
            _glowRect.localScale = _rect.localScale;
            _glowRect.sizeDelta = _rect.sizeDelta + new Vector2(glowExpand * 2, glowExpand * 2);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _seq?.Kill();
            _seq = DOTween.Sequence();
            _seq.Join(_rect.DOScale(_originScale * hoverScale, hoverDuration).SetEase(Ease.OutQuint));
            _seq.Join(_rect.DOLocalMoveY(_originPos.y + hoverLiftY, hoverDuration).SetEase(Ease.OutQuint));
            _seq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, hoverDuration).SetEase(Ease.OutQuint));

            if (_glowImage != null)
                _seq.Join(_glowImage.DOFade(1f, glowFadeDuration));
            if (_glowSkew != null)
                _seq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, hoverDuration).SetEase(Ease.OutQuint));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _seq?.Kill();
            _seq = DOTween.Sequence();
            _seq.Join(_rect.DOScale(_originScale, hoverDuration).SetEase(Ease.OutQuint));
            _seq.Join(_rect.DOLocalMoveY(_originPos.y, hoverDuration).SetEase(Ease.OutQuint));
            _seq.Join(DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, hoverDuration).SetEase(Ease.OutQuint));

            if (_glowImage != null)
                _seq.Join(_glowImage.DOFade(0f, glowFadeDuration));
            if (_glowSkew != null)
                _seq.Join(DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, hoverDuration).SetEase(Ease.OutQuint));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            _seq?.Kill();
            _rect.DOScale(_originScale * clickScale, clickDuration).SetEase(Ease.InQuad);
            DOTween.To(() => _skew.Skew, v => _skew.Skew = v, 0f, clickDuration).SetEase(Ease.OutQuint);
            if (_glowSkew != null)
                DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, 0f, clickDuration).SetEase(Ease.OutQuint);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enabled) return;
            if (_draggable != null && _draggable.IsDragging) return;

            bool stillHovering = _rect.localPosition.y > _originPos.y + 1f;
            float targetScale = stillHovering ? hoverScale : 1f;
            _rect.DOScale(_originScale * targetScale, releaseDuration).SetEase(Ease.OutQuint);

            if (stillHovering)
            {
                DOTween.To(() => _skew.Skew, v => _skew.Skew = v, hoverSkew, releaseDuration).SetEase(Ease.OutQuint);
                if (_glowSkew != null)
                    DOTween.To(() => _glowSkew.Skew, v => _glowSkew.Skew = v, -hoverSkew * glowSkewRatio, releaseDuration).SetEase(Ease.OutQuint);
            }
        }

        public void DestroyGlow()
        {
            _seq?.Kill(false);
            _seq = null;
            if (_glowSkew != null) DOTween.Kill(_glowSkew, false);
            if (_glowImage != null) DOTween.Kill(_glowImage, false);
            if (_glowRect != null && _glowRect.gameObject != null)
                Destroy(_glowRect.gameObject);
            _glowRect = null;
            _glowSkew = null;
            _glowImage = null;
        }

        private void LateUpdate()
        {
            SyncGlow();
        }

        private void OnDestroy()
        {
            _seq?.Kill(false);
            if (_glowSkew != null) DOTween.Kill(_glowSkew, false);
            if (_glowImage != null) DOTween.Kill(_glowImage, false);
            if (_glowRect != null && _glowRect.gameObject != null)
                Destroy(_glowRect.gameObject);
        }
    }
}
