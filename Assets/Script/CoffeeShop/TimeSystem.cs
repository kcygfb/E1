using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;

    public DayPhase CurrentPhase { get; private set; } = DayPhase.MorningCheck;

    [Header("Night Phase")]
    public string collectSceneName = "Collect";

    public static event Action<DayPhase, int> OnPhaseChanged;

    private static int savedDayCount = 1;

    private void Start()
    {
        dayCount = savedDayCount;
        Debug.Log($"[TimeSystem] Start() -> Day {dayCount}");
        EnterMorningCheck();
    }

    /// <summary>
    /// 玩家点击"结束今日营业"时调用
    /// </summary>
    public void EndShopPhase()
    {
        Debug.Log($"[TimeSystem] EndShopPhase -> Day {dayCount}");

        savedDayCount = dayCount;

        CurrentPhase = DayPhase.Night;
        EventBus.PublishPhaseChanged(DayPhase.Night, dayCount);
        OnPhaseChanged?.Invoke(DayPhase.Night, dayCount);

        EventBus.PublishDayEnded(dayCount);

        SceneManager.LoadScene(collectSceneName);
    }

    /// <summary>
    /// 夜晚收集结束，进入下一天（由 CollectSceneController 调用）
    /// 新 Cafe 场景加载后，TimeSystem.Start() 会读取 savedDayCount 并进入 MorningCheck
    /// </summary>
    public static void EndNightPhaseStatic()
    {
        savedDayCount++;
        Debug.Log($"[TimeSystem] EndNightPhaseStatic -> Day {savedDayCount}");
        SceneManager.LoadScene("Cafe");
    }

    /// <summary>
    /// 进入清点阶段：显示清点面板，预构建NPC队列，等待玩家确认
    /// </summary>
    private void EnterMorningCheck()
    {
        CurrentPhase = DayPhase.MorningCheck;

        Debug.Log($"[TimeSystem] EnterMorningCheck -> Day {dayCount}");

        EventBus.PublishPhaseChanged(DayPhase.MorningCheck, dayCount);
        OnPhaseChanged?.Invoke(DayPhase.MorningCheck, dayCount);
    }

    /// <summary>
    /// 玩家点击"开始营业"时调用，正式进入营业阶段
    /// </summary>
    public void StartShopPhase()
    {
        CurrentPhase = DayPhase.Shop;

        Debug.Log($"[TimeSystem] StartShopPhase -> Day {dayCount}");

        EventBus.PublishPhaseChanged(DayPhase.Shop, dayCount);
        OnPhaseChanged?.Invoke(DayPhase.Shop, dayCount);

        EventBus.PublishDayStarted(dayCount);
    }
}
