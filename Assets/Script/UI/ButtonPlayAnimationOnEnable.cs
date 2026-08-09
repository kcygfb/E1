using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>当 Button 从不 interactable 变为 interactable 时，启用 Animator 播放循环动画。
    /// 禁用时关闭 Animator。Animator 持续播放 loop clip，无状态切换 = 无卡顿。</summary>
    [RequireComponent(typeof(Animator))]
    public class ButtonPlayAnimationOnEnable : MonoBehaviour
    {
        private Button _button;
        private Animator _animator;
        private bool _wasInteractable;
        private bool _initialized;

        private void Awake()
        {
            _button = GetComponentInParent<Button>();
            _animator = GetComponent<Animator>();
            // 默认关闭，等按钮激活才开
            if (_animator != null) _animator.enabled = false;
        }

        private void Start()
        {
            if (_button != null)
            {
                _wasInteractable = _button.interactable;
                if (_wasInteractable && _animator != null)
                {
                    _animator.enabled = true;
                    _animator.Play("Play", 0, 0f);
                }
            }
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _button == null || _animator == null) return;

            bool interactable = _button.interactable;
            if (interactable != _wasInteractable)
            {
                _animator.enabled = interactable;
                if (interactable)
                {
                    _animator.Play("Play", 0, 0f);
                }
                _wasInteractable = interactable;
            }
        }
    }
}