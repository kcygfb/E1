using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>当 Button 从不 interactable 变为 interactable 时，触发一次播放动画。
    /// 挂载在显示用子物体上：自身持有 Image + Animator，Button 在父物体（无碰撞遮挡）。
    /// 配套 AnimatorController：Idle(默认空) + Play(触发)。动画不循环，播一次停最后一帧。</summary>
    [RequireComponent(typeof(Animator))]
    public class ButtonPlayAnimationOnEnable : MonoBehaviour
    {
        [Tooltip("触发参数名，默认 Play")]
        [SerializeField] private string triggerName = "Play";
        [Tooltip("布尔参数名（可选）。传入则用 bool 驱动而非 trigger，动画停在 Play 态")]
        [SerializeField] private string boolParameterName = "";

        private Button _button;
        private Animator _animator;
        private bool _wasInteractable;
        private bool _initialized;

        private void Awake()
        {
            _button = GetComponentInParent<Button>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (_button != null) _wasInteractable = _button.interactable;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _button == null || _animator == null) return;

            bool interactable = _button.interactable;
            if (interactable && !_wasInteractable)
            {
                _animator.Play("Idle", 0, 0f);
                if (!string.IsNullOrEmpty(boolParameterName))
                    _animator.SetBool(boolParameterName, true);
                else
                {
                    _animator.ResetTrigger(triggerName);
                    _animator.SetTrigger(triggerName);
                }
            }
            else if (!interactable && _wasInteractable)
            {
                if (!string.IsNullOrEmpty(boolParameterName))
                    _animator.SetBool(boolParameterName, false);
                _animator.Play("Idle", 0, 0f);
            }
            _wasInteractable = interactable;
        }
    }
}