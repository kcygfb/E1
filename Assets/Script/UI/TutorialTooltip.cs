using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        {            EnsureReferences();
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

        /// <summary>
        /// 把提示框放到屏幕坐标附近（用于悬停跟随鼠标）。offset 为相对该坐标的偏移。
        /// 只移动位置，不重建布局——布局只在 SetMessage/OnValidate 时计算，
        /// 避免每帧 RebuildLayout + ForceUpdateCanvases 造成布局抖动（频闪）。
        /// </summary>
        public void AttachToScreenPosition(Vector2 screenPosition, Vector2 offset)
        {
            EnsureReferences();
            if (_rectTransform == null)
                return;

            if (_rectTransform.parent is not RectTransform parent)
                return;

            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent, screenPosition, camera, out var localPoint))
                return;

            _rectTransform.anchoredPosition = localPoint + offset;
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
            messageText.overflowMode = TextOverflowModes.Overflow;

            var textRect = messageText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);

            var maxTextWidth = Mathf.Max(1f, maxWidth - padding.x * 2f);

            // 测量前先把文本区域撑大，避免 TMP 用当前（prefab 里的）小 rect 参与计算导致测不准。
            // 高度传一个很大的值而非 0：TMP 的 GetPreferredValues 中 0 表示"取当前 rect 尺寸"。
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.sizeDelta = new Vector2(maxTextWidth, 2048f);

            var preferred = messageText.GetPreferredValues(messageText.text, maxTextWidth, 2048f);
            var panelWidth = Mathf.Clamp(preferred.x + padding.x * 2f, minWidth, maxWidth);

            // 兜底：字体 atlas 未就绪等情况下测量可能返回 0，退化为最大宽度，保证长文本不被压缩。
            if (panelWidth <= minWidth && preferred.x < 1f && !string.IsNullOrWhiteSpace(messageText.text))
                panelWidth = maxWidth;

            var textWidth = Mathf.Max(1f, panelWidth - padding.x * 2f);
            var textHeight = messageText.GetPreferredValues(messageText.text, textWidth, 2048f).y;
            var panelHeight = Mathf.Max(minHeight, textHeight + padding.y * 2f);

            _rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);

            // 布局完成后恢复文本内边距（stretch 锚点下 offsetMin/offsetMax 即内边距）。
            textRect.offsetMin = padding;
            textRect.offsetMax = -padding;
            // 无需 ForceRebuildLayoutImmediate：尺寸全是手动设置，不依赖 Unity 自动布局系统，
            // 反而会在 OnValidate/Awake 期间触发 SendMessage 警告。
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
            if (messageText == null || _rectTransform == null)
                return;

            // OnValidate 期间不允许修改 RectTransform（会触发 SendMessage 警告），
            // 延迟到编辑器下一个 tick 再重建布局。
            UnityEditor.EditorApplication.delayCall += RebuildLayoutIfValid;
        }

        private void RebuildLayoutIfValid()
        {
            if (this == null || messageText == null || _rectTransform == null)
                return;
            RebuildLayout();
        }
#endif
    }
}
