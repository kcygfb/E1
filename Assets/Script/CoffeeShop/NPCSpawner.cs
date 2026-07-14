using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NPCSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DayNPCConfig
    {
        public int day;
        public List<NPCData> npcs;
    }

    [System.Serializable]
    private class NPCRequest
    {
        public NPCData npcData;
        public CoffeeData coffeeData;
    }

    [Header("Data")]
    public List<NPCData> npcPool = new();
    public List<CoffeeData> coffeePool = new();
    public OrderSystem orderSystem;

    [Header("Day Overrides")]
    [Tooltip("配置特定天数固定出场的NPC，不配置的天数走随机")]
    public List<DayNPCConfig> dayOverrides = new();

    [Header("Dialogue")]
    public DialogueManager dialogueManager;

    [Header("Daily Queue")]
    public int minNpcPerDay = 2;
    public int maxNpcPerDay = 4;

    [Header("Visual Spawn")]
    public GameObject npcVisualPrefab;
    public RectTransform npcParent;
    public Transform spawnPoint;
    public Transform counterPoint;
    public Transform exitPoint;

    [Header("End Day Button")]
    public GameObject endDayButton;

    [Header("Morning Check")]
    public GameObject morningCheckPanel;

    [Header("NPC Head Text")]
    public float headTextOffsetY = 120f;
    public int headTextFontSize = 24;

    private Queue<NPCRequest> waitingQueue = new();
    private NPCController currentNpc;

    private readonly HashSet<string> pendingReturnVisits = new();

    public bool HasPendingReturnVisit(NPCData npc)
    {
        return npc != null && pendingReturnVisits.Contains(npc.npcId);
    }

    public void MarkReturnVisit(NPCData npc)
    {
        if (npc != null)
        {
            pendingReturnVisits.Add(npc.npcId);
            Debug.Log($"[NPCSpawner] Marked return visit for {npc.npcName}");
        }
    }

    public void ClearReturnVisit(NPCData npc)
    {
        if (npc != null)
        {
            pendingReturnVisits.Remove(npc.npcId);
            Debug.Log($"[NPCSpawner] Cleared return visit for {npc.npcName}");
        }
    }

    public bool CanEndDay =>
        currentNpc == null &&
        waitingQueue.Count == 0 &&
        (orderSystem == null || !orderSystem.HasActiveOrder);

    private void OnEnable()
    {
        EventBus.DayStarted += OnDayStarted;
        EventBus.DayEnded += OnDayEnded;
        EventBus.PhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        EventBus.DayStarted -= OnDayStarted;
        EventBus.DayEnded -= OnDayEnded;
        EventBus.PhaseChanged -= OnPhaseChanged;
    }

    private void OnDayEnded(int day)
    {
        ClearAllNPCs();
    }

    private void ClearAllNPCs()
    {
        waitingQueue.Clear();

        if (currentNpc != null)
        {
            currentNpc.OnLeftStore -= HandleNpcLeft;
            Destroy(currentNpc.gameObject);
            currentNpc = null;
        }

        if (orderSystem != null && orderSystem.HasActiveOrder)
        {
            orderSystem.ClearActiveOrder();
        }

        Debug.Log("[NPCSpawner] Cleared all NPCs and active order");
    }

    private void Update()
    {
        if (endDayButton != null)
            endDayButton.SetActive(CanEndDay);
    }

    private void SetEndDayButtonVisible(bool visible)
    {
        if (endDayButton != null)
            endDayButton.SetActive(visible);
    }

    private void OnDayStarted(int day)
    {
        // Shop 阶段开始，NPC入场
        SetEndDayButtonVisible(false);
        if (morningCheckPanel != null)
            morningCheckPanel.SetActive(false);
        TrySpawnNextNpc();
    }

    private void OnPhaseChanged(DayPhase phase, int day)
    {
        if (phase == DayPhase.MorningCheck)
        {
            if (morningCheckPanel != null)
                morningCheckPanel.SetActive(true);
            SetEndDayButtonVisible(false);

            // 预构建队列（含回访检查）
            BuildQueueForDay(day);
        }
        else if (phase == DayPhase.Shop)
        {
            // Shop 阶段由 DayStarted 驱动 NPC 入场
        }
    }

    private void BuildQueueForDay(int day)
    {
        waitingQueue.Clear();

        // 1. 回访预检查：desiredCoffee 已解锁的 NPC 插入队列最前
        var returnVisitors = new List<NPCData>();
        var pendingIds = pendingReturnVisits.ToList();
        foreach (var npcId in pendingIds)
        {
            var npc = FindNPCById(npcId);
            if (npc == null || npc.desiredCoffee == null) continue;

            if (CoffeeUnlockManager.Instance != null &&
                CoffeeUnlockManager.Instance.IsUnlocked(npc.desiredCoffee))
            {
                returnVisitors.Add(npc);
                pendingReturnVisits.Remove(npcId);
                Debug.Log($"[NPCSpawner] Return visit: {npc.npcName} (coffee unlocked)");
            }
        }

        foreach (var npc in returnVisitors)
        {
            CoffeeData coffeeData = PickCoffeeFor(npc);
            waitingQueue.Enqueue(new NPCRequest
            {
                npcData = npc,
                coffeeData = coffeeData
            });
            Debug.Log($"[NPCSpawner] Return visit queue add -> {npc.npcName}");
        }

        // 2. 查 dayOverrides
        var overrideConfig = dayOverrides.FirstOrDefault(d => d.day == day);
        if (overrideConfig != null && overrideConfig.npcs != null && overrideConfig.npcs.Count > 0)
        {
            foreach (var npc in overrideConfig.npcs)
            {
                if (npc == null) continue;
                CoffeeData coffeeData = PickCoffeeFor(npc);
                waitingQueue.Enqueue(new NPCRequest
                {
                    npcData = npc,
                    coffeeData = coffeeData
                });
                Debug.Log($"[NPCSpawner] Day {day} override -> {npc.npcName} wants {(coffeeData != null ? coffeeData.coffeeName : "none")}");
            }
            Debug.Log($"[NPCSpawner] Day {day} queue count = {waitingQueue.Count} (override)");
            return;
        }

        // 3. 没有配置 → 走随机
        if (npcPool == null || npcPool.Count == 0)
        {
            Debug.LogError("[NPCSpawner] npcPool is empty.");
            return;
        }

        if (coffeePool == null || coffeePool.Count == 0)
        {
            Debug.LogError("[NPCSpawner] coffeePool is empty.");
            return;
        }

        var usedToday = new HashSet<NPCData>();

        var firstNpcs = npcPool.Where(n => n.spawnOrder == SpawnOrder.First).ToList();
        var lastNpcs = npcPool.Where(n => n.spawnOrder == SpawnOrder.Last).ToList();
        var randomPool = npcPool.Where(n => n.spawnOrder == SpawnOrder.Random).ToList();

        int total = Random.Range(minNpcPerDay, maxNpcPerDay + 1);
        int middleCount = Mathf.Max(0, total - firstNpcs.Count - lastNpcs.Count);

        // First NPCs
        foreach (var npc in firstNpcs)
        {
            EnqueueNpc(npc, usedToday);
        }

        // Random middle NPCs
        for (int i = 0; i < middleCount; i++)
        {
            var npc = PickRandomNpc(randomPool, usedToday);
            if (npc != null)
                EnqueueNpc(npc, usedToday);
        }

        // Last NPCs
        foreach (var npc in lastNpcs)
        {
            EnqueueNpc(npc, usedToday);
        }

        Debug.Log($"[NPCSpawner] Day {day} queue count = {waitingQueue.Count}");
    }

    private void EnqueueNpc(NPCData npcData, HashSet<NPCData> usedToday)
    {
        if (npcData == null || usedToday.Contains(npcData))
            return;

        usedToday.Add(npcData);

        CoffeeData coffeeData = PickCoffeeFor(npcData);
        waitingQueue.Enqueue(new NPCRequest
        {
            npcData = npcData,
            coffeeData = coffeeData
        });

        Debug.Log($"[NPCSpawner] Queue add -> {npcData.npcName} wants {(coffeeData != null ? coffeeData.coffeeName : "none")}");
    }

    private NPCData PickRandomNpc(List<NPCData> pool, HashSet<NPCData> usedToday)
    {
        var available = pool.Where(n => !usedToday.Contains(n)).ToList();
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    private void TrySpawnNextNpc()
    {
        if (currentNpc != null)
            return;

        if (orderSystem != null && orderSystem.HasActiveOrder)
            return;

        if (waitingQueue.Count == 0)
        {
            Debug.Log("[NPCSpawner] Queue empty, no more NPCs for now.");
            return;
        }

        NPCRequest request = waitingQueue.Dequeue();

        GameObject npcObj = CreateVisibleNpcObject(request);
        currentNpc = npcObj.GetComponent<NPCController>();
        currentNpc.OnLeftStore += HandleNpcLeft;
        currentNpc.Spawner = this;

        currentNpc.Initialize(
            request.npcData,
            request.coffeeData,
            orderSystem,
            dialogueManager,
            GetCounterPosition(),
            GetExitPosition()
        );

        currentNpc.headTextOffsetY = headTextOffsetY;
        currentNpc.headTextFontSize = headTextFontSize;
        currentNpc.RefreshHeadText();

        Debug.Log($"[NPCSpawner] Spawned -> {request.npcData.npcName} / {(request.coffeeData != null ? request.coffeeData.coffeeName : "none")}");
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
            obj = new GameObject(
                $"NPC_{request.npcData.npcName}",
                typeof(RectTransform),
                typeof(Image)
            );

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = request.npcData.portraitSize;
        }

        Transform parent = npcParent;
        if (parent == null)
        {
            var npcArea = GameObject.Find("NPCArea");
            if (npcArea != null)
                parent = npcArea.transform;
        }
        if (parent == null)
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                parent = canvas.transform;
        }
        if (parent != null)
            obj.transform.SetParent(parent, false);

        obj.transform.position = GetSpawnPosition();

        if (obj.GetComponent<NPCController>() == null)
        {
            obj.AddComponent<NPCController>();
        }

        obj.name = $"NPC_{request.npcData.npcName}";
        ApplyVisuals(obj, request);
        return obj;
    }

    private void ApplyVisuals(GameObject obj, NPCRequest request)
    {
        var img = obj.GetComponent<Image>();
        if (img == null) return;

        if (request.npcData.portrait != null)
        {
            img.sprite = request.npcData.portrait;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            img.color = Random.ColorHSV(
                0f, 1f,
                0.45f, 0.85f,
                0.65f, 1f
            );
        }
    }

    private void HandleNpcLeft(NPCController npc)
    {
        if (currentNpc == npc)
        {
            currentNpc.OnLeftStore -= HandleNpcLeft;
            currentNpc = null;
        }

        TrySpawnNextNpc();
    }

    private CoffeeData PickCoffeeFor(NPCData npcData)
    {
        // 特殊NPC：指定咖啡
        if (npcData.desiredCoffee != null)
            return npcData.desiredCoffee;

        // 普通NPC：从已解锁的里随机选
        var unlocked = coffeePool.Where(c => CoffeeUnlockManager.Instance != null && CoffeeUnlockManager.Instance.IsUnlocked(c)).ToList();
        if (unlocked.Count == 0)
        {
            Debug.LogWarning($"[NPCSpawner] No unlocked coffee for {npcData.npcName}");
            return null;
        }
        return unlocked[Random.Range(0, unlocked.Count)];
    }

    private NPCData FindNPCById(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;

        foreach (var npc in npcPool)
        {
            if (npc != null && npc.npcId == npcId)
                return npc;
        }

        foreach (var config in dayOverrides)
        {
            if (config.npcs == null) continue;
            foreach (var npc in config.npcs)
            {
                if (npc != null && npc.npcId == npcId)
                    return npc;
            }
        }

        return null;
    }

    private Vector3 GetSpawnPosition()
    {
        return spawnPoint != null ? spawnPoint.position : new Vector3(-6f, 1f, 0f);
    }

    private Vector3 GetCounterPosition()
    {
        return counterPoint != null ? counterPoint.position : new Vector3(0f, 1f, 0f);
    }

    private Vector3 GetExitPosition()
    {
        return exitPoint != null ? exitPoint.position : new Vector3(6f, 1f, 0f);
    }
}
