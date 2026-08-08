using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 教程框开关按钮：点击切换本场景教程框开关（TutorialController.CalloutsEnabled），
    /// 图标在 on/off 两张图之间切换。挂在任意 Button 上即可。
    /// 复制到其他场景后会自动找到该场景的 TutorialController，各场景独立控制。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class TutorialToggleButton : MonoBehaviour
    {
        [Tooltip("本场景的 TutorialController。留空则自动查找（FindFirstObjectByType）。")]
        [SerializeField] private TutorialController tutorialController;

        [Tooltip("显示开关状态的图标（通常是按钮自身的 Image）。")]
        [SerializeField] private Image iconImage;

        [Tooltip("开启状态图标。留空则自动使用启动时按钮当前显示的图。")]
        [SerializeField] private Sprite onSprite;

        [Tooltip("关闭状态图标。")]
        [SerializeField] private Sprite offSprite;

        private void Awake()
        {
            if (iconImage == null)
                iconImage = GetComponent<Image>();
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);

            // 按钮当前挂的图（教学状态on）即开启态图标。
            if (onSprite == null && iconImage != null)
                onSprite = iconImage.sprite;
        }

        private void Start()
        {
            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();

            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(Toggle);

            RefreshIcon(tutorialController != null && tutorialController.CalloutsEnabled);
        }

        private void OnDestroy()
        {
            var button = GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveListener(Toggle);
        }

        private void Toggle()
        {
            if (tutorialController == null)
                return;

            tutorialController.SetCalloutsEnabled(!tutorialController.CalloutsEnabled);
            RefreshIcon(tutorialController.CalloutsEnabled);
        }

        private void RefreshIcon(bool enabled)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = enabled ? onSprite : offSprite;
        }
    }
}
