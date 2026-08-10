using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>杯子。可拖动，接收MaterialIcon。
/// 放入材料后自动检查配方匹配，匹配则合并为完成咖啡icon。</summary>
[RequireComponent(typeof(Image))]
public class CupContainer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public List<string> Contents { get; } = new();
    public List<MaterialIcon> Icons { get; } = new();
    public bool IsFilled => Contents.Count > 0;

    /// <summary>合并后的咖啡ID（null=未合并，还是散装材料）。</summary>
    public string MergedCoffeeId { get; private set; }

    private Image _image;
    private Canvas _canvas;
    private CraftController _craftController;
    private Sprite _originalCupSprite;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _originalCupSprite = _image.sprite;
        _craftController = FindFirstObjectByType<CraftController>();
    }

    private void EnsureCanvas()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    public bool AcceptIcon(MaterialIcon icon)
    {
        if (icon == null) return false;

        Contents.Add(icon.MaterialId);
        Icons.Add(icon);
        icon.transform.SetParent(transform, false);
        icon.transform.localPosition = Vector3.zero;

        // 检查是否匹配配方
        TryMerge();
        return true;
    }

    /// <summary>检查杯内材料是否匹配某咖啡配方，匹配则合并。
    /// 也检查半成品：杯内材料是某咖啡 requiredMaterials 的真子集且匹配 halfProducts 定义时，切换为半成品外观。</summary>
    private void TryMerge()
    {
        if (CoffeeDataLoader.Instance == null || !CoffeeDataLoader.Instance.IsLoaded) return;

        var cupSet = new HashSet<string>(Contents);

        // 1. 检查完整配方匹配
        foreach (var coffee in CoffeeDataLoader.Instance.GetAllCoffees())
        {
            if (coffee.requiredMaterials == null || coffee.requiredMaterials.Count == 0) continue;
            var recipeSet = new HashSet<string>(coffee.requiredMaterials);
            if (!cupSet.SetEquals(recipeSet)) continue;

            // 匹配成功 → 合并
            MergedCoffeeId = coffee.coffeeId;

            // 合法咖啡一旦由玩家实际合成，就立即发现菜谱。
            // NPC 是否需要这杯咖啡只属于后续交付判定，不应影响菜谱解锁。
            if (KiKs.Combat.RuntimeGameRepository.DiscoverRecipeFromCrafting(coffee.coffeeId))
                Debug.Log($"[CupContainer] 解锁新菜谱: {coffee.coffeeName}");

            // 销毁所有子 icon
            foreach (var icon in Icons)
                if (icon != null) Destroy(icon.gameObject);
            Icons.Clear();

            // 杯子本身换成完成咖啡图
            var sprite = CoffeeIconCache.Instance?.GetCoffeeSprite(coffee.coffeeId);
            if (sprite != null)
            {
                _image.sprite = sprite;
                _image.preserveAspect = true;
                _image.color = Color.white;
            }

            Debug.Log($"[CupContainer] 合并为: {coffee.coffeeId}");
            return;
        }

        // 2. 检查半成品匹配（cupSet 是 requiredMaterials 的真子集 + 匹配 halfProducts 定义）
        if (MergedCoffeeId == null)
        {
            foreach (var coffee in CoffeeDataLoader.Instance.GetAllCoffees())
            {
                if (coffee.halfProducts == null || coffee.halfProducts.Count == 0) continue;
                if (coffee.requiredMaterials == null || coffee.requiredMaterials.Count == 0) continue;

                var fullSet = new HashSet<string>(coffee.requiredMaterials);
                // cupSet 必须是 fullSet 的真子集（还没完成）
                if (cupSet.Count >= fullSet.Count) continue;
                bool isSubset = true;
                foreach (var item in cupSet)
                    if (!fullSet.Contains(item)) { isSubset = false; break; }
                if (!isSubset) continue;

                // 检查是否匹配某个 halfProducts 定义
                foreach (var half in coffee.halfProducts)
                {
                    if (half.materials == null || half.materials.Count == 0) continue;
                    var halfSet = new HashSet<string>(half.materials);
                    if (cupSet.SetEquals(halfSet))
                    {
                        // 半成品匹配 → 销毁杯内 icon，切换杯子外观为半成品图
                        foreach (var icon in Icons)
                            if (icon != null) Destroy(icon.gameObject);
                        Icons.Clear();

                        var halfSprite = CoffeeIconCache.Instance?.GetHalfProductSprite(coffee.coffeeId);
                        if (halfSprite != null)
                        {
                            _image.sprite = halfSprite;
                            _image.preserveAspect = true;
                            _image.color = Color.white;
                        }
                        Debug.Log($"[CupContainer] 半成品匹配: {coffee.coffeeId} ({half.displayName})");
                        return;
                    }
                }
            }
        }
    }

    // --- 拖动 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureCanvas();
        if (_canvas != null) transform.SetParent(_canvas.transform, true);
        transform.SetAsLastSibling();
        if (_image != null) _image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_image != null) _image.raycastTarget = true;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 遍历所有命中，按优先级匹配第一个有效目标
        foreach (var r in results)
        {
            var go = r.gameObject;

            // CupStack → 放回销毁
            if (go.GetComponent<CupStack>() != null)
            {
                _craftController?.OnCupReturned(this);
                return;
            }

            // Btn_Deliver → 提交
            if (go.name == "Btn_Deliver")
            {
                _craftController?.OnCupDelivered(this);
                return;
            }

            // CustomerController → 给顾客
            if (go.GetComponent<CustomerController>() != null)
            {
                _craftController?.OnCupDelivered(this);
                return;
            }

            // NPCArea → 给顾客
            if (go.name == "NPCArea" || go.transform.root.name == "NPCArea")
            {
                _craftController?.OnCupDelivered(this);
                return;
            }
        }
    }

    public void RemoveIcon(MaterialIcon icon)
    {
        Icons.Remove(icon);
        var idx = Contents.IndexOf(icon.MaterialId);
        if (idx >= 0) Contents.RemoveAt(idx);
        // 取消合并状态，恢复杯子原图
        MergedCoffeeId = null;
        if (_image != null && _originalCupSprite != null)
        {
            _image.sprite = _originalCupSprite;
            _image.preserveAspect = true;
            _image.color = Color.white;
        }
    }

    public void ClearContents()
    {
        Contents.Clear();
        foreach (var icon in Icons)
            if (icon != null) Destroy(icon.gameObject);
        Icons.Clear();
        MergedCoffeeId = null;
        // 恢复杯子原图
        if (_image != null && _originalCupSprite != null)
        {
            _image.sprite = _originalCupSprite;
            _image.preserveAspect = true;
            _image.color = Color.white;
        }
    }
}
