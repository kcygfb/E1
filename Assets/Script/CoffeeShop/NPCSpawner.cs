using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCSpawner : MonoBehaviour
{
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

    [Header("NPC Head Text")]
    public float headTextOffsetY = 120f;
    public int headTextFontSize = 24;

    private Queue<NPCRequest> waitingQueue = new();
    private NPCController currentNpc;

    public bool CanEndDay =>
        currentNpc == null &&
        waitingQueue.Count == 0 &&
        (orderSystem == null || !orderSystem.HasActiveOrder);

    private void OnEnable()
    {
        EventBus.DayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        EventBus.DayStarted -= OnDayStarted;
    }

    private void OnDayStarted(int day)
    {
        BuildQueueForDay(day);
        TrySpawnNextNpc();
    }

    private void BuildQueueForDay(int day)
    {
        waitingQueue.Clear();

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

        int count = Random.Range(minNpcPerDay, maxNpcPerDay + 1);
        Debug.Log($"[NPCSpawner] Day {day} queue count = {count}");

        for (int i = 0; i < count; i++)
        {
            NPCData npcData = npcPool[Random.Range(0, npcPool.Count)];
            CoffeeData coffeeData = PickCoffeeFor(npcData);

            waitingQueue.Enqueue(new NPCRequest
            {
                npcData = npcData,
                coffeeData = coffeeData
            });

            Debug.Log($"[NPCSpawner] Queue add -> {npcData.npcName} wants {coffeeData.coffeeName}");
        }
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

        Debug.Log($"[NPCSpawner] Spawned -> {request.npcData.npcName} / {request.coffeeData.coffeeName}");
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
        return coffeePool[Random.Range(0, coffeePool.Count)];
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
