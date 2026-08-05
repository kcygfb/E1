using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>手柄停留区。挂到磨豆机/萃取机下作为 Portafilter 的停靠点。
/// Portafilter 拖到停留区上时吸附到停留区位置，并通知对应的 StepDropTarget 激活。</summary>
[RequireComponent(typeof(RectTransform))]
public class PortafilterDock : MonoBehaviour, IDropHandler
{
    [Header("Config")]
    [SerializeField] private string stepId;        // 对应的步骤 ID (Grind / Extract)
    [SerializeField] private RectTransform dockPoint; // 手柄停靠的位置（可为 null = 用自身位置）

    /// <summary>当前停靠在此的手柄（null = 空）。</summary>
    public Portafilter DockedPortafilter { get; private set; }

    public void OnDrop(PointerEventData eventData)
    {
        var pf = eventData.pointerDrag?.GetComponent<Portafilter>();
        if (pf == null) return;

        // 如果已有手柄停靠，不接受新的
        if (DockedPortafilter != null && DockedPortafilter != pf) return;

        // 吸附到停靠点
        var targetPos = dockPoint != null ? dockPoint.position : transform.position;
        pf.transform.position = targetPos;

        // 记录停靠
        DockedPortafilter = pf;
        pf.SetDock(this);

        Debug.Log($"[PortafilterDock] Portafilter docked at {stepId}");
    }

    /// <summary>手柄被拖走时调用。</summary>
    public void OnPortafilterLeft()
    {
        if (DockedPortafilter != null)
        {
            Debug.Log($"[PortafilterDock] Portafilter left {stepId}");
            DockedPortafilter = null;
        }
    }

    /// <summary>获取停靠点位置。</summary>
    public Vector3 GetDockPosition()
    {
        return dockPoint != null ? dockPoint.position : transform.position;
    }

    public string StepId => stepId;
}
