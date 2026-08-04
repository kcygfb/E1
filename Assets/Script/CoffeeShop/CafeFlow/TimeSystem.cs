using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.MorningCheck;

    [Header("Scene Flow")]
    public string preBattleSceneName = "PreBattle";

    private static int savedDayCount = 1;
    private DailyRevenueSummary dailyRevenueSummary;
    private bool isEndingShop;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.F9].wasPressedThisFrame
            && !isEndingShop)
        {
            Debug.Log("[TimeSystem] F9 debug skip -> CompleteEndShopPhase");
            CompleteEndShopPhase();
        }
    }

    private void Awake()
    {
        dailyRevenueSummary = GetComponent<DailyRevenueSummary>();
        if (dailyRevenueSummary == null)
            dailyRevenueSummary = gameObject.AddComponent<DailyRevenueSummary>();
    }

    private IEnumerator Start()
    {
        dayCount = savedDayCount;
        Debug.Log($"[TimeSystem] Start() -> Day {dayCount}");
        yield return KiKs.UI.TransitionEffect.WaitEntrance();
        EnterMorningCheck();
    }

    public void EndShopPhase()
    {
        if (isEndingShop) return;
        if (dailyRevenueSummary != null && dailyRevenueSummary.ShowSummary())
            return;

        CompleteEndShopPhase();
    }

    public void CompleteEndShopPhase()
    {
        if (isEndingShop) return;
        isEndingShop = true;
        savedDayCount = dayCount;
        CurrentPhase = DayPhase.Night;
        EmitPhaseChanged();
        GameEvent.Emit("DayEnded", dayCount);
        if (KiKs.UI.TransitionEffect.Instance != null)
            KiKs.UI.TransitionEffect.Instance.TransitionTo(preBattleSceneName);
        else
            SceneManager.LoadScene(preBattleSceneName);
    }

    public static void EndNightPhaseStatic()
    {
        savedDayCount++;
        Debug.Log($"[TimeSystem] EndNightPhaseStatic -> Day {savedDayCount}");
        SceneManager.LoadScene("Cafe");
    }

    public static void SetSavedDayCountForDemo(int day)
    {
        savedDayCount = Mathf.Max(1, day);
        Debug.Log($"[TimeSystem] Demo flow synced -> Day {savedDayCount}");
    }

    private void EnterMorningCheck()
    {
        CurrentPhase = DayPhase.MorningCheck;
        Debug.Log($"[TimeSystem] EnterMorningCheck -> Day {dayCount}");
        EmitPhaseChanged();
    }

    public void StartShopPhase()
    {
        CurrentPhase = DayPhase.Shop;
        Debug.Log($"[TimeSystem] StartShopPhase -> Day {dayCount}");
        EmitPhaseChanged();
        GameEvent.Emit("DayStarted", dayCount);
    }

    private void EmitPhaseChanged()
    {
        GameEvent.Emit("PhaseChanged", new PhaseChangedPayload { Phase = CurrentPhase, Day = dayCount });
    }
}
