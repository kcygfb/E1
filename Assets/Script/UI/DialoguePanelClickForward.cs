using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.UI
{
    /// <summary>点击对话面板任意区域 → 推进下一句。通过 DialogueBridge 静态事件，无需反射。</summary>
    public class DialoguePanelClickForward : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            DialogueBridge.OnAdvance?.Invoke();
        }
    }
}
