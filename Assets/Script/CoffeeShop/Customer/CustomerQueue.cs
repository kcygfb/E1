using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CustomerQueue : MonoBehaviour
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
    public List<DayNPCConfig> dayOverrides = new();

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

    private Queue<NPCRequest> waitingQueue = new();
    private CustomerController currentNpc;
    private readonly HashSet<string> pendingReturnVisits = new();

    public bool HasPendingReturnVisit(NPCData npc) =>
        npc != null && pendingReturnVisits.Contains(npc.npcId);

    public void MarkReturnVisit(NPCData npc)
    {
        if (npc != null)
        {
            pendingReturnVisits.Add(npc.npcId);
            Debug.Log($"[CustomerQueue] Marked return visit for {npc.npcName}");
        }
    }

    public void ClearReturnVisit(NPCData npc)
    {
        if (npc != null)
        {
            pendingReturnVisits.Remove(npc.npcId);
            Debug.Log($"[CustomerQueue] Cleared return visit for {npc.npcName}");
        }
    }

    public bool CanEndDay =>
        currentNpc == null &&
        waitingQueue.Count == 0 &&
        (orderSystem == null || !orderSystem.HasActiveOrder);

    private void OnEnable()
    {
        GameEvent.On("DayStarted", OnDayStarted);
        GameEvent.On("DayEnded", OnDayEnded);
        GameEvent.On("PhaseChanged", OnPhaseChanged);
    }

    private void OnDisable()
    {
        GameEvent.Off("DayStarted", OnDayStarted);
        GameEvent.Off("DayEnded", OnDayEnded);
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
    }

    private void OnDayStarted(object payload) { /* int day — not used, spawn driven by PhaseChanged */ }

    private void OnDayEnded(object payload) { ClearAllNPCs(); }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        if (p.Phase == DayPhase.MorningCheck)
        {
            if (morningCheckPanel != null) morningCheckPanel.SetActive(true);
            if (endDayButton != null) endDayButton.SetActive(false);
            BuildQueueForDay(p.Day);
        }
        else if (p.Phase == DayPhase.Shop)
        {
            if (morningCheckPanel != null) morningCheckPanel.SetActive(false);
            if (endDayButton != null) endDayButton.SetActive(false);
            TrySpawnNextNpc();
        }
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
            orderSystem.ClearActiveOrder();
    }

    private void Update()
    {
        if (endDayButton != null)
            endDayButton.SetActive(CanEndDay);
    }

    private void BuildQueueForDay(int day)
    {
        waitingQueue.Clear();

        // 1. 回访预检查
        var pendingIds = pendingReturnVisits.ToList();
        foreach (var npcId in pendingIds)
        {
            var npc = FindNPCById(npcId);
            if (npc == null || string.IsNullOrEmpty(npc.desiredCoffeeId)) continue;
            if (UnlockManager.Instance != null)
            {
                var loader = CoffeeDataLoader.Instance;
                if (loader != null && loader.IsLoaded)
                {
                    var json = loader.GetCoffee(npc.desiredCoffeeId);
                    if (json != null)
                    {
                        var tempData = ScriptableObject.CreateInstance<CoffeeData>();
                        tempData.ApplyJson(json);
                        if (UnlockManager.Instance.IsUnlocked(tempData))
                        {
                            returnVisitors.Add(npc);
                            pendingReturnVisits.Remove(npcId);
                        }
                        Destroy(tempData);
                    }
                }
            }
        }

        // 2. Day overrides
        var overrideConfig = dayOverrides.FirstOrDefault(d => d.day == day);
        if (overrideConfig != null && overrideConfig.npcs != null && overrideConfig.npcs.Count > 0)
        {
            foreach (var npc in overrideConfig.npcs)
            {
                if (npc == null) continue;
                EnqueueNpc(npc, null);
            }
            return;
        }

        // 3. Random
        if (npcPool == null || npcPool.Count == 0) return;

        var usedToday = new HashSet<NPCData>();
        var firstNpcs = npcPool.Where(n => n.spawnOrder == SpawnOrder.First).ToList();
        var lastNpcs = npcPool.Where(n => n.spawnOrder == SpawnOrder.Last).ToList();
        var randomPool = npcPool.Where(n => n.spawnOrder == SpawnOrder.Random).ToList();

        int total = Random.Range(minNpcPerDay, maxNpcPerDay + 1);
        int middleCount = Mathf.Max(0, total - firstNpcs.Count - lastNpcs.Count);

        foreach (var npc in firstNpcs) EnqueueNpc(npc, usedToday);
        for (int i = 0; i < middleCount; i++)
        {
            var npc = PickRandomNpc(randomPool, usedToday);
            if (npc != null) EnqueueNpc(npc, usedToday);
        }
        foreach (var npc in lastNpcs) EnqueueNpc(npc, usedToday);
    }

    private List<NPCData> returnVisitors = new();

    private void EnqueueNpc(NPCData npcData, HashSet<NPCData> usedToday)
    {
        if (npcData == null || (usedToday != null && usedToday.Contains(npcData))) return;
        if (usedToday != null) usedToday.Add(npcData);

        CoffeeData coffeeData = PickCoffeeFor(npcData);
        waitingQueue.Enqueue(new NPCRequest { npcData = npcData, coffeeData = coffeeData });
    }

    private NPCData PickRandomNpc(List<NPCData> pool, HashSet<NPCData> usedToday)
    {
        var available = pool.Where(n => !usedToday.Contains(n)).ToList();
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    private void TrySpawnNextNpc()
    {
        if (currentNpc != null) return;
        if (orderSystem != null && orderSystem.HasActiveOrder) return;
        if (waitingQueue.Count == 0) return;

        var request = waitingQueue.Dequeue();
        GameObject npcObj = CreateVisibleNpcObject(request);
        currentNpc = npcObj.GetComponent<CustomerController>();
        currentNpc.OnLeftStore += HandleNpcLeft;
        currentNpc.Spawner = this;
        currentNpc.Initialize(request.npcData, request.coffeeData, GetCounterPosition(), GetExitPosition());
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
        if (request.npcData.portrait != null)
        {
            img.sprite = request.npcData.portrait;
            img.color = Color.white;
            img.preserveAspect = true;
        }
        else
        {
            img.color = Random.ColorHSV(0f, 1f, 0.45f, 0.85f, 0.65f, 1f);
        }
    }

    private void HandleNpcLeft(CustomerController npc)
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
        if (!string.IsNullOrEmpty(npcData.desiredCoffeeId))
        {
            var loader = CoffeeDataLoader.Instance;
            if (loader != null && loader.IsLoaded)
            {
                var json = loader.GetCoffee(npcData.desiredCoffeeId);
                if (json != null)
                {
                    var coffee = ScriptableObject.CreateInstance<CoffeeData>();
                    coffee.ApplyJson(json);
                    return coffee;
                }
            }
            return null;
        }

        var unlocked = coffeePool.Where(c => UnlockManager.Instance != null && UnlockManager.Instance.IsUnlocked(c)).ToList();
        if (unlocked.Count == 0) return null;
        return unlocked[Random.Range(0, unlocked.Count)];
    }

    private NPCData FindNPCById(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        foreach (var npc in npcPool)
            if (npc != null && npc.npcId == npcId) return npc;
        foreach (var config in dayOverrides)
        {
            if (config.npcs == null) continue;
            foreach (var npc in config.npcs)
                if (npc != null && npc.npcId == npcId) return npc;
        }
        return null;
    }

    private Vector3 GetSpawnPosition() => spawnPoint != null ? spawnPoint.position : new Vector3(-6f, 1f, 0f);
    private Vector3 GetCounterPosition() => counterPoint != null ? counterPoint.position : new Vector3(0f, 1f, 0f);
    private Vector3 GetExitPosition() => exitPoint != null ? exitPoint.position : new Vector3(6f, 1f, 0f);
}
