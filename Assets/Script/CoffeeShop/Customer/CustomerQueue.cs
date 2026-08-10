using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顾客队列控制器。从 DayConfig 读取配置，生成顾客/收尾 NPC。
/// 无 DayConfig 的天 = 纯随机日，用 GenericCustomer 刷。
/// </summary>
public class CustomerQueue : MonoBehaviour
{
    [System.Serializable]
    private class NPCRequest
    {
        public NPCData npcData;
        public NPCEntry entry;
        public CoffeeData coffeeData;
        public Sprite portrait;
        public bool acceptAny;
    }

    [Header("Day Configs")]
    [Tooltip("剧情天的配置。无配置的天=随机刷通用顾客。")]
    public List<DayConfig> dayConfigs = new();

    [Header("Random Day Fallback")]
    [Tooltip("随机日使用的通用顾客 NPCData")]
    public NPCData genericCustomer;
    public int minNpcPerDay = 2;
    public int maxNpcPerDay = 4;

    [Header("System")]
    public OrderSystem orderSystem;

    [Header("Visual Spawn")]
    public GameObject npcVisualPrefab;
    public RectTransform npcParent;
    public Transform spawnPoint;
    public Transform counterPoint;
    public Transform exitPoint;
    [Tooltip("NPC移动速度")]
    public float npcMoveSpeed = 500f;

    [Header("End Day Button")]
    public GameObject endDayButton;

    private Queue<NPCRequest> waitingQueue = new();
    private Queue<NPCRequest> endOfDayQueue = new();
    private CustomerController currentNpc;
    private TimeSystem _timeSystem;

    private bool _shopPhaseActive;
    private bool _endOfDayPhaseActive;

    private bool IsShopQueueEmpty =>
        currentNpc == null &&
        waitingQueue.Count == 0 &&
        (orderSystem == null || !orderSystem.HasActiveOrder);

    /// <summary>供 StartOfDayController 调用</summary>
    public DayConfig GetDayConfig(int day)
    {
        if (dayConfigs == null) return null;
        return dayConfigs.FirstOrDefault(d => d != null && d.day == day);
    }

    private void OnEnable()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        GameEvent.On("DayEnded", OnDayEnded);
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        GameEvent.Off("DayEnded", OnDayEnded);
    }

    private void OnDayEnded(object payload) { ClearAllNPCs(); }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;

        if (p.Phase == DayPhase.Shop)
        {
            _shopPhaseActive = true;
            _endOfDayPhaseActive = false;
            if (endDayButton != null) endDayButton.SetActive(false);
            BuildQueueForDay(p.Day);
            TrySpawnNextNpc();
            NotifyIfShopComplete();
        }
        else if (p.Phase == DayPhase.EndOfDay)
        {
            _shopPhaseActive = false;
            _endOfDayPhaseActive = true;
            if (endDayButton != null) endDayButton.SetActive(false);
            BuildEndOfDayQueue(p.Day);
            TrySpawnNextNpc();
        }
        else if (p.Phase == DayPhase.MorningCheck)
        {
            if (endDayButton != null) endDayButton.SetActive(false);
        }
    }

    private void ClearAllNPCs()
    {
        waitingQueue.Clear();
        endOfDayQueue.Clear();
        if (currentNpc != null)
        {
            currentNpc.OnLeftStore -= HandleNpcLeft;
            Destroy(currentNpc.gameObject);
            currentNpc = null;
        }
        if (orderSystem != null && orderSystem.HasActiveOrder)
            orderSystem.ClearActiveOrder();
        _shopPhaseActive = false;
        _endOfDayPhaseActive = false;
    }

    private void Update()
    {
        if (endDayButton != null)
            endDayButton.SetActive(_shopPhaseActive && IsShopQueueEmpty);
    }

    // ==================== Build Queues ====================

    private void BuildQueueForDay(int day)
    {
        waitingQueue.Clear();

        var config = GetDayConfig(day);

        if (config != null && config.customers != null && config.customers.Count > 0)
        {
            foreach (var entry in config.customers)
            {
                if (entry == null || entry.npc == null) continue;
                EnqueueNpc(entry);
            }
            return;
        }

        // 无 DayConfig：随机刷 GenericCustomer
        if (genericCustomer == null) return;
        int total = Random.Range(minNpcPerDay, maxNpcPerDay + 1);
        for (int i = 0; i < total; i++)
        {
            var entry = new NPCEntry
            {
                npc = genericCustomer,
                orderMode = OrderMode.RandomUnlocked
            };
            EnqueueNpc(entry);
        }
    }

    private void BuildEndOfDayQueue(int day)
    {
        endOfDayQueue.Clear();

        var config = GetDayConfig(day);

        if (config == null || config.endOfDay == null || config.endOfDay.Count == 0)
        {
            NotifyEndOfDayComplete();
            return;
        }

        foreach (var entry in config.endOfDay)
        {
            if (entry == null || entry.npc == null) continue;
            var coffeeData = PickCoffeeFor(entry);
            endOfDayQueue.Enqueue(new NPCRequest
            {
                npcData = entry.npc,
                entry = entry,
                coffeeData = coffeeData,
                portrait = PickPortrait(entry.npc),
                acceptAny = false
            });
        }
    }

    // ==================== Enqueue ====================

    private Sprite _lastPortrait;

    private void EnqueueNpc(NPCEntry entry)
    {
        if (entry == null || entry.npc == null) return;

        Sprite portrait = PickPortrait(entry.npc);

        CoffeeData coffeeData = PickCoffeeFor(entry);
        bool acceptAny = entry.orderMode == OrderMode.AcceptAny;

        waitingQueue.Enqueue(new NPCRequest
        {
            npcData = entry.npc,
            entry = entry,
            coffeeData = coffeeData,
            portrait = portrait,
            acceptAny = acceptAny
        });
    }

    private Sprite PickPortrait(NPCData npcData)
    {
        Sprite portrait = npcData.portrait;
        if (npcData.portraitPool != null && npcData.portraitPool.Count > 0)
        {
            var valid = npcData.portraitPool.FindAll(p => p != null);
            if (valid.Count > 0)
            {
                if (valid.Count > 1 && _lastPortrait != null)
                    valid = valid.FindAll(p => p != _lastPortrait);
                portrait = valid[Random.Range(0, valid.Count)];
                _lastPortrait = portrait;
            }
        }
        return portrait;
    }

    // ==================== Spawn ====================

    private void TrySpawnNextNpc()
    {
        if (currentNpc != null) return;
        if (orderSystem != null && orderSystem.HasActiveOrder) return;

        NPCRequest request;
        bool isEndOfDay = false;

        if (_shopPhaseActive && waitingQueue.Count > 0)
        {
            request = waitingQueue.Dequeue();
        }
        else if (_endOfDayPhaseActive && endOfDayQueue.Count > 0)
        {
            request = endOfDayQueue.Dequeue();
            isEndOfDay = true;
        }
        else
        {
            if (_shopPhaseActive) NotifyShopComplete();
            else if (_endOfDayPhaseActive) NotifyEndOfDayComplete();
            return;
        }

        GameObject npcObj = CreateVisibleNpcObject(request);
        currentNpc = npcObj.GetComponent<CustomerController>();
        currentNpc.OnLeftStore += HandleNpcLeft;
        currentNpc.Spawner = this;
        currentNpc.moveSpeed = npcMoveSpeed;
        currentNpc.AcceptAny = request.acceptAny;
        currentNpc.Initialize(request.npcData, request.entry, request.coffeeData, GetCounterPosition(), GetExitPosition());

        if (isEndOfDay)
        {
            currentNpc.MarkEndOfDayDialogue();
        }
    }

    private GameObject CreateVisibleNpcObject(NPCRequest request)
    {
        GameObject obj;
        if (npcVisualPrefab != null)
        {
            obj = Instantiate(npcVisualPrefab);
        }
        else
        {
            obj = new GameObject($"NPC_{request.npcData.npcName}", typeof(RectTransform), typeof(Image));
            obj.GetComponent<RectTransform>().sizeDelta = request.npcData.portraitSize;
        }

        Transform parent = npcParent;
        if (parent == null)
        {
            var npcArea = GameObject.Find("NPCArea");
            if (npcArea != null) parent = npcArea.transform;
        }
        if (parent == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) parent = canvas.transform;
        }
        if (parent != null) obj.transform.SetParent(parent, false);

        obj.transform.position = GetSpawnPosition();

        if (obj.GetComponent<CustomerController>() == null)
            obj.AddComponent<CustomerController>();

        obj.name = $"NPC_{request.npcData.npcName}";
        ApplyVisuals(obj, request);
        return obj;
    }

    private void ApplyVisuals(GameObject obj, NPCRequest request)
    {
        var img = obj.GetComponent<Image>();
        if (img == null) return;
        Sprite portrait = request.portrait != null ? request.portrait : request.npcData.portrait;
        if (portrait != null)
        {
            img.sprite = portrait;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            img.color = Random.ColorHSV(0f, 1f, 0.45f, 0.85f, 0.65f, 1f);
        }
    }

    // ==================== Callbacks ====================

    private void HandleNpcLeft(CustomerController npc)
    {
        if (currentNpc == npc)
        {
            currentNpc.OnLeftStore -= HandleNpcLeft;
            currentNpc = null;
        }
        TrySpawnNextNpc();
    }

    private void NotifyIfShopComplete()
    {
        if (_shopPhaseActive && IsShopQueueEmpty)
            NotifyShopComplete();
    }

    private void NotifyShopComplete()
    {
        if (_timeSystem == null)
            _timeSystem = FindFirstObjectByType<TimeSystem>();
        if (_timeSystem != null)
            _timeSystem.NotifyAllCustomersServed();
    }

    private void NotifyEndOfDayComplete()
    {
        if (_timeSystem == null)
            _timeSystem = FindFirstObjectByType<TimeSystem>();
        if (_timeSystem != null)
            _timeSystem.NotifyEndOfDayComplete();
    }

    // ==================== Coffee Picking ====================

    private CoffeeData PickCoffeeFor(NPCEntry entry)
    {
        if (entry == null) return null;

        switch (entry.orderMode)
        {
            case OrderMode.AcceptAny:
                return null;

            case OrderMode.SpecificCoffee:
                if (!string.IsNullOrEmpty(entry.coffeeId))
                {
                    var loader = CoffeeDataLoader.Instance;
                    if (loader != null && loader.IsLoaded)
                    {
                        var json = loader.GetCoffee(entry.coffeeId);
                        if (json != null)
                        {
                            var coffee = ScriptableObject.CreateInstance<CoffeeData>();
                            coffee.ApplyJson(json);
                            return coffee;
                        }
                    }
                }
                return null;

            case OrderMode.RandomUnlocked:
            default:
                var loader2 = CoffeeDataLoader.Instance;
                if (loader2 != null && loader2.IsLoaded)
                {
                    var unlocked = loader2.GetAllCoffees()
                        .Where(c => !c.locked && KiKs.Combat.RuntimeGameRepository.IsRecipeUnlocked(c.coffeeId))
                        .ToList();
                    if (unlocked.Count > 0)
                    {
                        var picked = unlocked[Random.Range(0, unlocked.Count)];
                        var coffee = ScriptableObject.CreateInstance<CoffeeData>();
                        coffee.ApplyJson(picked);
                        return coffee;
                    }
                }
                return null;
        }
    }

    // ==================== Positions ====================

    private Vector3 GetSpawnPosition() => spawnPoint != null ? spawnPoint.position : new Vector3(-6f, 1f, 0f);
    private Vector3 GetCounterPosition() => counterPoint != null ? counterPoint.position : new Vector3(0f, 1f, 0f);
    private Vector3 GetExitPosition() => exitPoint != null ? exitPoint.position : new Vector3(6f, 1f, 0f);

    public Vector3 GetSpawnPositionPublic() => GetSpawnPosition();
    public Vector3 GetCounterPositionPublic() => GetCounterPosition();
    public Vector3 GetExitPositionPublic() => GetExitPosition();
}
