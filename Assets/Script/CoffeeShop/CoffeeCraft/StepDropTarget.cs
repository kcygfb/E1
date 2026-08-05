using UnityEngine;
using UnityEngine.UI;

/// <summary>步骤按钮接收拖拽材料。挂到步骤按钮（Btn_Grind / Btn_Pour 等）上。</summary>
[RequireComponent(typeof(Image))]
public class StepDropTarget : MonoBehaviour
{
    public string stepId;

    internal void OnDropMaterial(string materialId)
    {
        if (string.IsNullOrEmpty(stepId) || string.IsNullOrEmpty(materialId)) return;

        var cc = FindFirstObjectByType<CraftController>();
        if (cc != null)
            cc.OnMaterialDroppedOnStep(stepId, materialId);
    }
}