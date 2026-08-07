using TMPro;
using UnityEngine;

namespace KiKs.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialTooltip : MonoBehaviour
    {
        public enum Placement
        {
            Above,
            Below,
            Left,
            Right,
            Center
        }

        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Vector2 padding = new Vector2(24f, 16f);
        [SerializeField] private float minWidth = 140f;
        [SerializeField] private float maxWidth = 380f;
        [SerializeField] private float minHeight = 70f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            EnsureReferences();
        }

        public void SetMessage(string message)
        {
            EnsureReferences();
            if (messageText == null)
            {
                Debug.LogWarning("[TutorialTooltip] Missing TMP text reference.", this);
                return;
            }

            messageText.text = message ?? string.Empty;
            RebuildLayout();
        }

        public void AttachTo(RectTransform target, Placement placement, Vector2 offset)
        {
            EnsureReferences();
            if (_rectTransform == null || target == null)
                return;

            RebuildLayout();
            Canvas.ForceUpdateCanvases();

            var targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);

            var targetCenter = (targetCorners[0] + targetCorners[2]) * 0.5f;
            var targetWidth = Vector3.Distance(targetCorners[0], targetCorners[3]);
            var targetHeight = Vector3.Distance(targetCorners[0], targetCorners[1]);
            var tooltipWidth = _rectTransform.rect.width * _rectTransform.lossyScale.x;
            var tooltipHeight = _rectTransform.rect.height * _rectTransform.lossyScale.y;

            var position = targetCenter;
            switch (placement)
            {
                case Placement.Above:
                    position += target.up * (targetHeight * 0.5f + tooltipHeight * 0.5f);
                    break;
                case Placement.Below:
                    position -= target.up * (targetHeight * 0.5f + tooltipHeight * 0.5f);
                    break;
                case Placement.Left:
                    position -= target.right * (targetWidth * 0.5f + tooltipWidth * 0.5f);
                    break;
                case Placement.Right:
                    position += target.right * (targetWidth * 0.5f + tooltipWidth * 0.5f);
                    break;
            }

            _rectTransform.position = position;
            _rectTransform.anchoredPosition += offset;
            ClampToParent();
        }

        public void RebuildLayout()
        {
            EnsureReferences();
            if (_rectTransform == null || messageText == null)
                return;

            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.enableAutoSizing = false;
            messageText.margin = Vector4.zero;

            var textRect = messageText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = padding;
            textRect.offsetMax = -padding;

            var availableTextWidth = Mathf.Max(1f, maxWidth - padding.x * 2f);
            var preferred = messageText.GetPreferredValues(messageText.text, availableTextWidth, 0f);
            var panelWidth = Mathf.Clamp(preferred.x + padding.x * 2f, minWidth, maxWidth);

            var textWidth = Mathf.Max(1f, panelWidth - padding.x * 2f);
            var textHeight = messageText.GetPreferredValues(messageText.text, textWidth, 0f).y;
            var panelHeight = Mathf.Max(minHeight, textHeight + padding.y * 2f);

            _rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);
        }

        private void ClampToParent()
        {
            if (_rectTransform.parent is not RectTransform parent)
                return;

            var halfSize = _rectTransform.rect.size * 0.5f;
            var position = _rectTransform.anchoredPosition;
            var parentRect = parent.rect;

            position.x = Mathf.Clamp(position.x, parentRect.xMin + halfSize.x, parentRect.xMax - halfSize.x);
            position.y = Mathf.Clamp(position.y, parentRect.yMin + halfSize.y, parentRect.yMax - halfSize.y);
            _rectTransform.anchoredPosition = position;
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureReferences();
            if (messageText != null && _rectTransform != null)
                RebuildLayout();
        }
#endif
    }
}
