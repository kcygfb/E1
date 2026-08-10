using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经营前（开场）对话控制器。
/// 从 CustomerQueue.GetDayConfig(day).startOfDay 读取 NPCEntry 列表，
/// 依次出场播放对话，完成后回调 TimeSystem.NotifyStartOfDayComplete()。
/// 无配置或无 DayConfig 时直接推进。
/// </summary>
public class StartOfDayController : MonoBehaviour
{
    private TimeSystem _timeSystem;
    private CustomerQueue _queue;
    private bool _played;

    private void Awake()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        _timeSystem = FindFirstObjectByType<TimeSystem>();
    }

    private void OnDestroy()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        if (p.Phase == DayPhase.Night)
        {
            _played = false;
            return;
        }
        if (p.Phase != DayPhase.StartOfDay) return;
        if (_played) return;
        _played = true;

        _queue = FindFirstObjectByType<CustomerQueue>();

        var entries = ResolveEntriesForCurrentDay();
        if (entries == null || entries.Count == 0)
        {
            CompleteStartOfDay();
            return;
        }

        StartCoroutine(PlayStartOfDayDialogues(entries));
    }

    private List<NPCEntry> ResolveEntriesForCurrentDay()
    {
        if (_queue == null) return null;
        var config = _queue.GetCurrentDayConfig();
        if (config == null || config.startOfDay == null || config.startOfDay.Count == 0)
            return null;
        return config.startOfDay;
    }

    private System.Collections.IEnumerator PlayStartOfDayDialogues(List<NPCEntry> entries)
    {
        // 隐藏 MorningCheck 相关 UI
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.HideAll();
        var morningCheckPanel = GameObject.Find("Canvas/MorningCheckPanel");
        if (morningCheckPanel != null) morningCheckPanel.SetActive(false);

        foreach (var entry in entries)
        {
            if (entry == null || entry.npc == null) continue;

            // 生成 NPC
            var npcObj = CreateNpc(entry.npc);
            var controller = npcObj.GetComponent<CustomerController>();
            if (controller == null) controller = npcObj.AddComponent<CustomerController>();

            controller.Spawner = _queue;
            controller.moveSpeed = _queue != null ? _queue.npcMoveSpeed : 500f;

            var spawnPos = _queue != null ? _queue.GetSpawnPositionPublic() : new Vector3(-6f, 1f, 0f);
            var counterPos = _queue != null ? _queue.GetCounterPositionPublic() : new Vector3(0f, 1f, 0f);
            var exitPos = _queue != null ? _queue.GetExitPositionPublic() : new Vector3(6f, 1f, 0f);

            controller.Initialize(entry.npc, entry, null, counterPos, exitPos);
            npcObj.transform.position = spawnPos;
            controller.MarkStartOfDayDialogue();

            // 等 NPC 离开
            bool left = false;
            controller.OnLeftStore += _ => left = true;
            yield return new WaitUntil(() => left);
        }

        // 恢复 MorningCheck 相关 UI（TimeSystem.EnterMorningCheck 也会处理，这里确保即时）
        var mcPanel = GameObject.Find("Canvas/MorningCheckPanel");
        if (mcPanel != null) mcPanel.SetActive(true);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.ShowSelection();

        CompleteStartOfDay();
    }

    private void CompleteStartOfDay()
    {
        if (_timeSystem == null)
            _timeSystem = FindFirstObjectByType<TimeSystem>();
        if (_timeSystem != null)
            _timeSystem.NotifyStartOfDayComplete();
        else
            Debug.LogWarning("[StartOfDayController] TimeSystem not found.");
    }

    private GameObject CreateNpc(NPCData npcData)
    {
        GameObject obj;

        if (_queue != null)
        {
            var prefab = _queue.npcVisualPrefab;
            if (prefab != null)
            {
                obj = Instantiate(prefab);
            }
            else
            {
                obj = new GameObject($"NPC_{npcData.npcName}", typeof(RectTransform), typeof(Image));
                obj.GetComponent<RectTransform>().sizeDelta = npcData.portraitSize;
            }
        }
        else
        {
            obj = new GameObject($"NPC_{npcData.npcName}", typeof(RectTransform), typeof(Image));
            obj.GetComponent<RectTransform>().sizeDelta = npcData.portraitSize;
        }

        Transform parent = null;
        if (_queue != null)
        {
            parent = _queue.npcParent;
        }
        if (parent == null)
        {
            var npcArea = GameObject.Find("NPCArea");
            if (npcArea != null) parent = npcArea.transform;
        }
        if (parent == null) parent = FindFirstObjectByType<Canvas>().transform;
        obj.transform.SetParent(parent, false);

        var img = obj.GetComponent<Image>();
        if (img != null)
        {
            if (npcData.portrait != null)
            {
                img.sprite = npcData.portrait;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.color = Random.ColorHSV(0f, 1f, 0.45f, 0.85f, 0.65f, 1f);
            }
        }

        return obj;
    }
}
