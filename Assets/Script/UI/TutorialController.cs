using System;
using System.Collections.Generic;
using UnityEngine;

namespace KiKs.UI
{
    /// <summary>
    /// 挂在场景空物体上。
    /// Scene Callouts 由 Inspector 手动配置；程序化对象通过 RegisterJsonCallout 读取 JSON 配置。
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

            public TutorialTooltip.Placement placement = TutorialTooltip.Placement.Above;
            public Vector2 offset = new Vector2(0f, 20f);
            public bool showOnStart = true;
        }

        [Header("Tooltip")]
        [SerializeField] private TutorialTooltip tooltipPrefab;
        [SerializeField] private RectTransform tooltipParent;

        [Header("Scene Callouts")]
        [SerializeField] private bool showSceneCalloutsOnStart = true;
        [SerializeField] private SceneCallout[] sceneCallouts = Array.Empty<SceneCallout>();

        [Header("Behavior")]
        [SerializeField] private bool refreshPositionsEveryFrame = true;

        private readonly List<ActiveCallout> _activeCallouts = new();

        private sealed class ActiveCallout
        {
            public readonly Component Owner;
            public readonly RectTransform Target;
            public readonly TutorialTooltip Tooltip;
            public readonly TutorialTooltip.Placement Placement;
            public readonly Vector2 Offset;
            public readonly bool IsJsonCallout;

            public ActiveCallout(
                Component owner,
                RectTransform target,
                TutorialTooltip tooltip,
                TutorialTooltip.Placement placement,
                Vector2 offset,
                bool isJsonCallout)
            {
                Owner = owner;
                Target = target;
                Tooltip = tooltip;
                Placement = placement;
                Offset = offset;
                IsJsonCallout = isJsonCallout;
            }
        }

        private void Start()
        {
            if (showSceneCalloutsOnStart)
                ShowSceneCallouts();
        }

        private void LateUpdate()
        {
            for (var i = _activeCallouts.Count - 1; i >= 0; i--)
            {
                var callout = _activeCallouts[i];
                if (callout.Target == null || callout.Tooltip == null)
                {
                    DestroyTooltip(callout.Tooltip);
                    _activeCallouts.RemoveAt(i);
                    continue;
                }

                var targetIsVisible = callout.Target.gameObject.activeInHierarchy;
                if (callout.Tooltip.gameObject.activeSelf != targetIsVisible)
                    callout.Tooltip.gameObject.SetActive(targetIsVisible);

                if (targetIsVisible && refreshPositionsEveryFrame)
                    callout.Tooltip.AttachTo(callout.Target, callout.Placement, callout.Offset);
            }
        }

        private void OnDisable()
        {
            HideAll();
        }

        /// <summary>显示 Inspector 中所有勾选 Show On Start 的场景对象提示。</summary>
        public void ShowSceneCallouts()
        {
            ClearCallouts(isJsonCallout: false, owner: null);

            foreach (var callout in sceneCallouts)
            {
                if (callout == null || !callout.showOnStart || callout.target == null)
                    continue;

                CreateCallout(
                    owner: null,
                    target: callout.target,
                    description: callout.description,
                    placement: callout.placement,
                    offset: callout.offset,
                    isJsonCallout: false);
            }
        }

        /// <summary>
        /// 程序化对象创建后调用。提示文字、位置和偏移全部读取对应 JSON 的 tutorial 字段。
        /// </summary>
        public void RegisterJsonCallout(
            Component owner,
            RectTransform target,
            TutorialHintJson tutorial)
        {
            if (owner == null || target == null)
                return;

            RemoveJsonCallout(owner, target);

            if (tutorial == null || string.IsNullOrWhiteSpace(tutorial.description))
                return;

            CreateCallout(
                owner,
                target,
                tutorial.description,
                GetPlacement(tutorial.placement),
                new Vector2(tutorial.offsetX, tutorial.offsetY),
                isJsonCallout: true);
        }

        /// <summary>程序化 UI 被隐藏、回收或销毁时调用，移除其教学提示。</summary>
        public void UnregisterJsonCallouts(Component owner)
        {
            if (owner == null)
                return;

            ClearCallouts(isJsonCallout: true, owner: owner);
        }

        /// <summary>隐藏当前 Controller 创建的全部教学提示。</summary>
        public void HideAll()
        {
            for (var i = _activeCallouts.Count - 1; i >= 0; i--)
            {
                DestroyTooltip(_activeCallouts[i].Tooltip);
                _activeCallouts.RemoveAt(i);
            }
        }

        private void CreateCallout(
            Component owner,
            RectTransform target,
            string description,
            TutorialTooltip.Placement placement,
            Vector2 offset,
            bool isJsonCallout)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            if (tooltipPrefab == null)
            {
                Debug.LogWarning("[TutorialController] Assign Tooltip Prefab in the Inspector.", this);
                return;
            }

            var parent = ResolveTooltipParent();
            if (parent == null)
            {
                Debug.LogWarning("[TutorialController] Assign Tooltip Parent in the Inspector.", this);
                return;
            }

            var tooltip = Instantiate(tooltipPrefab, parent, false);
            tooltip.name = $"Tutorial_{target.name}";
            tooltip.SetMessage(description);

            if (target.gameObject.activeInHierarchy)
                tooltip.AttachTo(target, placement, offset);
            else
                tooltip.gameObject.SetActive(false);

            tooltip.transform.SetAsLastSibling();

            _activeCallouts.Add(new ActiveCallout(
                owner,
                target,
                tooltip,
                placement,
                offset,
                isJsonCallout));
        }

        private void RemoveJsonCallout(Component owner, RectTransform target)
        {
            for (var i = _activeCallouts.Count - 1; i >= 0; i--)
            {
                var callout = _activeCallouts[i];
                if (!callout.IsJsonCallout || callout.Owner != owner || callout.Target != target)
                    continue;

                DestroyTooltip(callout.Tooltip);
                _activeCallouts.RemoveAt(i);
            }
        }

        private void ClearCallouts(bool isJsonCallout, Component owner)
        {
            for (var i = _activeCallouts.Count - 1; i >= 0; i--)
            {
                var callout = _activeCallouts[i];
                if (callout.IsJsonCallout != isJsonCallout ||
                    (owner != null && callout.Owner != owner))
                {
                    continue;
                }

                DestroyTooltip(callout.Tooltip);
                _activeCallouts.RemoveAt(i);
            }
        }

        private RectTransform ResolveTooltipParent()
        {
            if (tooltipParent != null)
                return tooltipParent;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();

            return canvas != null ? canvas.transform as RectTransform : null;
        }

        private static TutorialTooltip.Placement GetPlacement(string placement)
        {
            return placement?.Trim().ToLowerInvariant() switch
            {
                "below" => TutorialTooltip.Placement.Below,
                "left" => TutorialTooltip.Placement.Left,
                "right" => TutorialTooltip.Placement.Right,
                "center" => TutorialTooltip.Placement.Center,
                _ => TutorialTooltip.Placement.Above
            };
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