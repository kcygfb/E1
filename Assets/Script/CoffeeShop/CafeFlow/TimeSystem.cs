using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.MorningCheck;

    [Header("Night Phase")]
    public string collectSceneName = "Collect";

    private static int savedDayCount = 1;

    private void Start()
    {
        dayCount = savedDayCount;
        Debug.Log($"[TimeSystem] Start() -> Day {dayCount}");
        EnterMorningCheck();
    }

    public void EndShopPhase()
    {
        savedDayCount = dayCount;
        CurrentPhase = DayPhase.Night;
        EmitPhaseChanged();
        GameEvent.Emit("DayEnded", dayCount);
        SceneManager.LoadScene(collectSceneName);
    }

    public static void EndNightPhaseStatic()
    {
        savedDayCount++;
        Debug.Log($"[TimeSystem] EndNightPhaseStatic -> Day {savedDayCount}");
        SceneManager.LoadScene("Cafe");
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
