using UnityEngine;
using UnityEngine.EventSystems;
using System.Reflection;

namespace KiKs.UI
{
    /// <summary>挂到 DialoguePanel 上，点击面板任意区域转发到 DialoguePlayer 下一句</summary>
    public class DialoguePanelClickForward : MonoBehaviour, IPointerClickHandler
    {
        private MonoBehaviour _player;
        private System.Type _playerType;
        private MethodInfo _onNext;
        private PropertyInfo _isRunning;

        private void Start()
        {
            _playerType = System.Type.GetType("DialoguePlayer, Assembly-CSharp");
            if (_playerType == null)
            {
                // Fallback: search all types
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    _playerType = asm.GetType("DialoguePlayer");
                    if (_playerType != null) break;
                }
            }
            if (_playerType != null)
            {
                _onNext = _playerType.GetMethod("OnNextClicked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _isRunning = _playerType.GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_player == null && _playerType != null)
                _player = FindFirstObjectByType(_playerType) as MonoBehaviour;

            if (_player == null || _isRunning == null || _onNext == null) return;

            bool running = (bool)_isRunning.GetValue(_player);
            if (running)
                _onNext.Invoke(_player, null);
        }
    }
}
