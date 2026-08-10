using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 主菜单「制作人名单」面板控制。
    /// 挂在场景任意 GameObject 上，按名字自动查找并绑定：
    /// <list type="bullet">
    /// <item>打开按钮：场景中名为 "制作人名单" 的 Button</item>
    /// <item>面板：打开按钮下名为 "Image" 的子物体</item>
    /// <item>关闭按钮：面板内名为 "退出" 的 Button</item>
    /// </list>
    /// 无需在 Inspector 手动拖引用；默认进入场景后面板处于关闭状态。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CreditsPanelController : MonoBehaviour
    {
        [Tooltip("打开名单面板的按钮物体名")]
        [SerializeField] private string openButtonName = "制作人名单";

        [Tooltip("名单图所在子物体名（打开按钮的子物体）")]
        [SerializeField] private string panelObjectName = "Image";

        [Tooltip("名单图内关闭按钮的物体名")]
        [SerializeField] private string closeButtonName = "退出";

        private GameObject _panel;

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 在整个场景查找打开按钮（含其未激活子物体，但按钮自身需激活）
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Button openButton = null;
            foreach (var b in buttons)
            {
                if (b != null && b.name == openButtonName)
                {
                    openButton = b;
                    break;
                }
            }

            if (openButton == null)
            {
                Debug.LogWarning($"[CreditsPanel] 场景中未找到名为 \"{openButtonName}\" 的 Button。", this);
                return;
            }

            // 面板 = 打开按钮下名为 panelObjectName 的子物体（含未激活）
            var panelTransform = FindChildByName(openButton.transform, panelObjectName);
            if (panelTransform == null)
            {
                Debug.LogWarning($"[CreditsPanel] \"{openButtonName}\" 下未找到名为 \"{panelObjectName}\" 的子物体。", this);
                return;
            }
            _panel = panelTransform.gameObject;

            // 关闭按钮
            Button closeButton = null;
            var closeTransform = FindChildByName(panelTransform, closeButtonName);
            if (closeTransform != null)
                closeTransform.TryGetComponent(out closeButton);

            if (closeButton == null)
                Debug.LogWarning($"[CreditsPanel] 未在 \"{panelObjectName}\" 内找到名为 \"{closeButtonName}\" 的 Button。", this);

            // 绑定事件（先移除防止重复绑定）
            openButton.onClick.RemoveListener(Show);
            openButton.onClick.AddListener(Show);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            // 默认关闭
            _panel.SetActive(false);
        }

        /// <summary>显示制作人名单。</summary>
        public void Show()
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            // 置顶渲染，避免被主菜单其他元素遮挡
            _panel.transform.SetAsLastSibling();
        }

        /// <summary>关闭制作人名单。</summary>
        public void Hide()
        {
            if (_panel == null) return;
            _panel.SetActive(false);
        }

        /// <summary>递归查找指定名字的子物体（含未激活）。</summary>
        private static Transform FindChildByName(Transform parent, string targetName)
        {
            if (parent.name == targetName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildByName(parent.GetChild(i), targetName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
