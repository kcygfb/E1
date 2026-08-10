using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经营前对话控制器。
/// 拦截 MorningCheck 阶段：先隐藏 MorningCheck 面板，依次出场 NPC 播放 startOfDayDialogueId，
/// 全部对话结束后再显示 MorningCheck 面板，让正常流程继续。
/// 不修改 TimeSystem，通过监听 PhaseChanged 事件工作。
/// </summary>
public class StartOfDayController : MonoBehaviour
{
    [System.Serializable]
    public class DayNPCConfig
    {
        public int day;
        public List<NPCData> npcs;
    }

    [Header("经营前 NPC（任意天）")]
    [Tooltip("每天经营前都出场的 NPC。")]
    public List<NPCData> startOfDayNpcs = new();

    [Header("天数覆盖")]
    [Tooltip("特定天数的经营前 NPC。优先于上面的默认列表。")]
    public List<DayNPCConfig> dayOverrides = new();

    private CustomerQueue _queue;
    private GameObject _morningCheckPanel;
    private bool _played;

    private void OnEnable()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        if (p.Phase == DayPhase.Night)
        {
            _played = false; // 新的一天重置
            return;
        }
        if (p.Phase != DayPhase.MorningCheck) return;
        if (_played) return;
        _played = true;

        _queue = FindFirstObjectByType<CustomerQueue>();
        _morningCheckPanel = _queue != null ? _queue.morningCheckPanel : null;

        // 按天数选 NPC 列表：dayOverrides 优先，否则用默认 startOfDayNpcs
        var npcs = ResolveNpcsForDay(p.Day);
        if (npcs == null || npcs.Count == 0)
            return;

        if (_morningCheckPanel != null)
            _morningCheckPanel.SetActive(false);

        StartCoroutine(PlayStartOfDayDialogues(npcs));
    }

    private List<NPCData> ResolveNpcsForDay(int day)
    {
        if (dayOverrides != null)
        {
            foreach (var config in dayOverrides)
            {
                if (config != null && config.day == day && config.npcs != null && config.npcs.Count > 0)
                    return config.npcs;
            }
        }
        return startOfDayNpcs;
    }

    private System.Collections.IEnumerator PlayStartOfDayDialogues(List<NPCData> npcs)
    {
        yield return null;

        foreach (var npcData in npcs)
        {
            if (npcData == null) continue;
            if (string.IsNullOrEmpty(npcData.startOfDayDialogueId)) continue;

            // 生成 NPC
            var npcObj = CreateNpc(npcData);
            var controller = npcObj.GetComponent<CustomerController>();
            if (controller == null) controller = npcObj.AddComponent<CustomerController>();

            controller.Spawner = _queue;
            controller.moveSpeed = _queue != null ? _queue.npcMoveSpeed : 500f;

            var spawnPos = _queue != null ? _queue.GetSpawnPositionPublic() : new Vector3(-6f, 1f, 0f);
            var counterPos = _queue != null ? _queue.GetCounterPositionPublic() : new Vector3(0f, 1f, 0f);
            var exitPos = _queue != null ? _queue.GetExitPositionPublic() : new Vector3(6f, 1f, 0f);

            controller.Initialize(npcData, null, counterPos, exitPos);
            npcObj.transform.position = spawnPos;
            controller.MarkStartOfDayDialogue();

            // 等 NPC 离开
            bool left = false;
            controller.OnLeftStore += _ => left = true;
            yield return new WaitUntil(() => left);
        }

        // 全部对话完毕，显示 MorningCheck 面板
        if (_morningCheckPanel != null)
            _morningCheckPanel.SetActive(true);
    }

    private GameObject CreateNpc(NPCData npcData)
    {
        GameObject obj;

        // 尝试用 CustomerQueue 的 prefab
        if (_queue != null)
        {
            var prefabField = typeof(CustomerQueue).GetField("npcVisualPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var prefab = prefabField?.GetValue(_queue) as GameObject;
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

        // 设置父物体
        Transform parent = null;
        if (_queue != null)
        {
            var parentField = typeof(CustomerQueue).GetField("npcParent",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            parent = parentField?.GetValue(_queue) as Transform;
        }
        if (parent == null)
        {
            var npcArea = GameObject.Find("NPCArea");
            if (npcArea != null) parent = npcArea.transform;
        }
        if (parent == null) parent = FindFirstObjectByType<Canvas>().transform;
        obj.transform.SetParent(parent, false);

        // 设置立绘
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
