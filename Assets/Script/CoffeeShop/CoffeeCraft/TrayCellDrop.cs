using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>九宫格格子接收拖拽。MorningCheck 阶段接收材料列表拖入，Shop 阶段禁用。</summary>
[RequireComponent(typeof(Image))]
public class TrayCellDrop : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public int cellIndex;

    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged material from palette item or tray slot
        string materialId = null;

        // From MaterialPaletteItem
        var paletteItem = eventData.pointerDrag?.GetComponent<MaterialPaletteItem>();
        if (paletteItem != null)
            materialId = paletteItem.MaterialId;

        if (string.IsNullOrEmpty(materialId)) return;

        if (TrayGridUI.Instance != null)
            TrayGridUI.Instance.OnMaterialDroppedOnCell(cellIndex, materialId);
    }

    /// <summary>右键/左键点击空格子清除当前材料。</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (TrayGridUI.Instance != null)
                TrayGridUI.Instance.ClearCell(cellIndex);
        }
    }
}
