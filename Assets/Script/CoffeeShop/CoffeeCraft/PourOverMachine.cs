using UnityEngine;

/// <summary>手冲机器。壶槽即材料槽——壶放在KettleSlot上同时接收材料。
/// CanStart: 壶在槽 + 壶里有材料。处理: 从壶取材料查配方 → 有配方产出指定物品，无配方产出Unknown。</summary>
public class PourOverMachine : CraftMachine
{
    [SerializeField] private KettleSlot kettleSlot;

    protected override bool CanStart()
    {
        if (kettleSlot == null || !kettleSlot.IsFilled) return false;
        var kettle = kettleSlot.Current;
        if (kettle == null || !kettle.IsFilled) return false;
        return craftController != null && !craftController.IsProcessing;
    }

    protected override void OnStartClicked()
    {
        if (!CanStart()) return;

        var kettle = kettleSlot.Current;
        if (kettle == null) return;

        var inputId = kettle.GetMaterialId();
        if (string.IsNullOrEmpty(inputId)) return;

        // 查不到配方 → 产出 Unknown
        string outputId;
        if (!MachineRecipeLibrary.TryGetOutput(machineId, inputId, out outputId))
        {
            outputId = "Unknown";
            Debug.Log($"[PourOverMachine] {machineId} + {inputId} 无配方，产出 Unknown");
        }

        // 消耗壶里的材料
        kettle.ConsumeAll();
        // 产出倒回壶
        kettle.AddContent(outputId);

        craftController.OnMachineComplete(machineId);
    }

    protected override void ProduceOutput(string outputMaterialId) { }

    public override void ResetMachine() { }
}
