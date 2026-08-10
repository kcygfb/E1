using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 挂在场景空物体上。所有教学提示统一为"悬停目标显示、跟随鼠标"：
    /// 程序化对象通过 RegisterJsonCallout 读取 JSON 配置；场景静态对象通过 Scene Callouts 配置。
    /// 唯一的注册入口是 RegisterJsonCallout，全部走同一套悬停逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialController : MonoBehaviour
    {
        [Serializable]
        public sealed class SceneCallout
        {
            [Tooltip("从 Hierarchy 直接拖入场景中已有的 UI 对象。")]
            public RectTransform target;

            [TextArea(2, 5)]
            public string description;

            [Tooltip("提示框相对鼠标的偏移。默认 (0, 48) 显示在鼠标上方；Offset 为 0 时同样视为未配置，自动显示在鼠标上方。")]
            public Vector2 offset = new Vector2(0f, 48f);

            public bool showOnStart = true;
        }

        [Header("Tooltip")]
        [SerializeField] private TutorialTooltip tooltipPrefab;
        [SerializeField] private RectTransform tooltipParent;
        [SerializeField, Min(0.5f)] private float tooltipScale = 1f;

        [Header("Scene Callouts")]
        [SerializeField] private bool showSceneCalloutsOnStart = true;
        [SerializeField] private SceneCallout[] sceneCallouts = Array.Empty<SceneCallout>();

        private readonly List<HoverCallout> _callouts = new();

        private bool _calloutsEnabled = true;

        /// <summary>本场景教程框开关。关闭时本场景所有悬停提示不再显示，已显示的立即隐藏。</summary>
        public bool CalloutsEnabled => _calloutsEnabled;

        public void SetCalloutsEnabled(bool enabled)
        {
            if (_calloutsEnabled == enabled)
                return;

            _calloutsEnabled = enabled;
            if (!_calloutsEnabled)
            {
                // 关闭开关时立即隐藏当前所有已显示的提示框（保留注册，重新开启后恢复悬停显示）。
                HideAllTooltips();
            }
        }

        private void HideAllTooltips()
        {
            foreach (var callout in _callouts)
            {
                if (callout != null && callout.Tooltip != null)
                    callout.Tooltip.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 挂在目标 UI 上的悬停监听。指针进入时显示提示框并记录鼠标位置，移出时隐藏；
        /// 提示框的位置跟随由 TutorialController.LateUpdate 统一驱动。
        /// </summary>
        private sealed class HoverCallout : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
        {
            public TutorialController Controller;
            public Component Owner;
            public TutorialTooltip Tooltip;
            public Vector2 Offset;
            public Vector2 LastMousePosition;

            public static HoverCallout Attach(RectTransform target, TutorialTooltip tooltip, Vector2 offset, TutorialController controller)
            {
                var callout = target.GetComponent<HoverCallout>();
                if (callout == null)
                    callout = target.gameObject.AddComponent<HoverCallout>();
                callout.Controller = controller;
                callout.Tooltip = tooltip;
                callout.Offset = offset;
                return callout;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (Tooltip == null)
                    return;

                // 场景级开关：本场景关闭时悬停不显示。
                if (Controller != null && !Controller.CalloutsEnabled)
                    return;

                LastMousePosition = eventData.position;
                Tooltip.gameObject.SetActive(true);
                Tooltip.transform.SetAsLastSibling();
                Tooltip.AttachToScreenPosition(LastMousePosition, Offset);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                // 立即隐藏，不延迟：提示框已禁用射线检测，不会遮挡目标造成 enter/exit 循环；
                // 延迟反而会在鼠标快速滑过相邻物品时留下旧框残影。
                if (Tooltip != null)
                    Tooltip.gameObject.SetActive(false);
            }

            public void OnPointerMove(PointerEventData eventData)
            {
                LastMousePosition = eventData.position;
            }
        }

        private void Start()
        {
            if (showSceneCalloutsOnStart)
                ShowSceneCallouts();
        }

        private void LateUpdate()
        {
            for (var i = _callouts.Count - 1; i >= 0; i--)
            {
                var callout = _callouts[i];
                if (callout == null || callout.Tooltip == null)
                {
                    _callouts.RemoveAt(i);
                    continue;
                }

                if (!callout.Tooltip.gameObject.activeSelf)
                    continue;

                callout.Tooltip.AttachToScreenPosition(callout.LastMousePosition, callout.Offset);
            }
        }

        private void OnDisable()
        {
            HideAll();
        }

        /// <summary>注册 Inspector 中所有勾选 Show On Start 的场景对象悬停提示。</summary>
        public void ShowSceneCallouts()
        {
            for (var i = _callouts.Count - 1; i >= 0; i--)
            {
                if (_callouts[i].Owner == null)
                    DestroyCalloutAt(i);
            }

            foreach (var callout in sceneCallouts)
            {
                if (callout == null || !callout.showOnStart || callout.target == null)
                    continue;

                RegisterCallout(null, callout.target, callout.description, callout.offset);
            }
        }

        /// <summary>
        /// 唯一的公共注册入口。提示文字与偏移读取对应 JSON 的 tutorial 字段，
        /// 悬停 target 时显示并跟随鼠标；description 为空则不注册。
        /// </summary>
        public void RegisterJsonCallout(
            Component owner,
            RectTransform target,
            TutorialHintJson tutorial)
        {
            if (owner == null || target == null)
                return;

            // One owner can create an entire list of targets (for example all cafe materials).
            // Registering one target must only replace that target's previous callout; otherwise
            // later entries overwrite the earlier ones and an entry without tutorial data clears all.
            UnregisterJsonCallout(owner, target);

            if (tutorial == null || string.IsNullOrWhiteSpace(tutorial.description))
                return;

            RegisterCallout(
                owner,
                target,
                tutorial.description,
                new Vector2(tutorial.offsetX, tutorial.offsetY));
        }

        /// <summary>移除 owner 注册的全部悬停提示（owner 为 null 时移除场景静态提示）。</summary>
        private void UnregisterJsonCallout(Component owner, RectTransform target)
        {
            for (var i = _callouts.Count - 1; i >= 0; i--)
            {
                var callout = _callouts[i];
                if (callout != null && callout.Owner == owner && callout.transform == target)
                    DestroyCalloutAt(i);
            }
        }

        public void UnregisterJsonCallouts(Component owner)
        {
            for (var i = _callouts.Count - 1; i >= 0; i--)
            {
                if (_callouts[i].Owner == owner)
                    DestroyCalloutAt(i);
            }
        }

        /// <summary>隐藏并销毁全部教学提示。</summary>
        public void HideAll()
        {
            for (var i = _callouts.Count - 1; i >= 0; i--)
                DestroyCalloutAt(i);
        }

        /// <summary>默认提示框位置：鼠标上方 48px（offset 未配置时使用）。</summary>
        private static readonly Vector2 DefaultOffset = new(0f, 48f);

        private void RegisterCallout(Component owner, RectTransform target, string description, Vector2 offset)
        {
            if (string.IsNullOrWhiteSpace(description) || target == null)
                return;

            // offset 为 0 视为未配置，默认显示在鼠标上方，避免提示框正中盖住鼠标。
            if (offset.x == 0f && offset.y == 0f)
                offset = DefaultOffset;

            var tooltip = CreateTooltipInstance(description, target);
            if (tooltip == null)
                return;

            tooltip.name = $"Tutorial_{target.name}";
            tooltip.gameObject.SetActive(false);

            var callout = HoverCallout.Attach(target, tooltip, offset, this);
            callout.Owner = owner;
            _callouts.Add(callout);
        }

        private TutorialTooltip CreateTooltipInstance(string description, RectTransform target)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            if (tooltipPrefab == null)
            {
                Debug.LogWarning("[TutorialController] Assign Tooltip Prefab in the Inspector.", this);
                return null;
            }

            var parent = ResolveTooltipParent(target);
            if (parent == null)
            {
                Debug.LogWarning("[TutorialController] Assign Tooltip Parent in the Inspector.", this);
                return null;
            }

            var tooltip = Instantiate(tooltipPrefab, parent, false);
            tooltip.SetScale(tooltipScale);
            tooltip.SetMessage(description);
            DisableRaycastTargets(tooltip.gameObject);
            return tooltip;
        }

        /// <summary>
        /// 关闭提示框上所有 Graphic 的射线检测。
        /// 否则提示框遮挡目标时会把鼠标事件抢走，导致目标 enter/exit 反复切换（频闪）。
        /// </summary>
        private static void DisableRaycastTargets(GameObject root)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void DestroyCalloutAt(int index)
        {
            var callout = _callouts[index];
            _callouts.RemoveAt(index);

            if (callout == null)
                return;

            if (callout.Tooltip != null)
                DestroyTooltip(callout.Tooltip);
            Destroy(callout);
        }

        private RectTransform ResolveTooltipParent(RectTransform target)
        {
            if (tooltipParent != null)
                return tooltipParent;

            // Keep the tooltip in the target's screen-space coordinate system. This avoids
            // editor Game-view scaling and multiple-Canvas coordinate mismatches.
            var targetCanvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace)
                return targetCanvas.rootCanvas.transform as RectTransform;

            // 优先选非 WorldSpace 的 Canvas：WorldSpace Canvas 会把提示框挂进 3D 世界，屏幕上看不到。
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                    return canvas.transform as RectTransform;
            }

            var fallback = FindFirstObjectByType<Canvas>();
            return fallback != null ? fallback.transform as RectTransform : null;
        }

        private static void DestroyTooltip(TutorialTooltip tooltip)
        {
            if (tooltip == null)
                return;

            tooltip.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(tooltip.gameObject);
        }
    }
}
