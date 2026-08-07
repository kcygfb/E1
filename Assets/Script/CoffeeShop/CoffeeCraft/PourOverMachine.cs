using System.Collections.Generic;
using UnityEngine;

/// <summary>手冲机器。壶槽即材料槽——壶放在KettleSlot上同时接收材料。
/// 多输入配方：壶里需同时有 GroundCoffee + Water 才产出 PourOverCoffee。</summary>
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

        // 用壶里全部材料做多输入配方匹配
        string outputId;
        if (!MachineRecipeLibrary.TryGetOutputMulti(machineId, kettle.Contents, out outputId))
        {
            outputId = "Unknown";
            Debug.Log($"[PourOverMachine] {machineId} + [{string.Join(", ", kettle.Contents)}] 无配方，产出 Unknown");
        }

        // 消耗壶里的材料
        kettle.ConsumeAll();

        // 检测壶附近是否有杯子 → 有杯子直接进杯子，没杯子倒回壶
        var cup = FindCupAtKettle(kettle);
        if (cup != null)
        {
            if (materialIconPrefab == null)
                materialIconPrefab = CreateDefaultIconPrefab();

            var iconGO = Instantiate(materialIconPrefab, cup.transform, false);
            iconGO.transform.localPosition = Vector3.zero;
            var icon = iconGO.GetComponent<MaterialIcon>();
            if (icon == null) icon = iconGO.AddComponent<MaterialIcon>();
            icon.Setup(outputId);
            cup.AcceptIcon(icon);
            Debug.Log($"[PourOverMachine] {machineId} 产出 {outputId} → 直接进杯子");
        }
        else
        {
            // 没杯子 → 产出倒回壶
            kettle.AddContent(outputId);
            Debug.Log($"[PourOverMachine] {machineId} 产出 {outputId} → 倒回壶");
        }

        craftController.OnMachineComplete(machineId);
    }

    /// <summary>检测壶位置是否有杯子重叠。</summary>
    private CupContainer FindCupAtKettle(Kettle kettle)
    {
        var kettleRT = kettle.GetComponent<RectTransform>();
        if (kettleRT == null) return null;

        var kettleRect = GetWorldRect(kettleRT);
        var cups = FindObjectsByType<CupContainer>(FindObjectsSortMode.None);
        foreach (var cup in cups)
        {
            if (cup == null) continue;
            var cupRT = cup.GetComponent<RectTransform>();
            if (cupRT == null) continue;
            var cupRect = GetWorldRect(cupRT);
            if (kettleRect.Overlaps(cupRect))
                return cup;
        }
        return null;
    }

    protected override void ProduceOutput(string outputMaterialId) { }

    public override void ResetMachine() { }
}
