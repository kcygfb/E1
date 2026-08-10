using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cafe scene flow controller.  Also performs a one-time cleanup of dangling
/// components that may exist in the scene file (see case 1422705322/1422705323).
/// </summary>
public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.MorningCheck;

    [Header("Scene Flow")]
    public string preBattleSceneName = "PreBattle";

    [Header("Morning Check")]
    [SerializeField] private GameObject morningCheckPanel;
    [SerializeField] private Button startShopBtn;

    private DailyRevenueSummary dailyRevenueSummary;
    private bool isEndingShop;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CleanupDanglingComponents()
    {
        if (SceneManager.GetActiveScene().name != "Cafe") return;

        // Remove any RectTransform or MonoBehaviour that is not attached to a GameObject.
        // These are leftovers from an earlier scene edit and cause console errors.
        var all = Object.FindObjectsByType<Component>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c == null) continue;
            if (c.gameObject == null)
            {
                Debug.LogWarning($"[TimeSystem] Removing dangling component: {c.GetType().Name}");
                Object.DestroyImmediate(c);
            }
        }
    }

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

        // Auto-find via Canvas hierarchy (works even if MorningCheckPanel is inactive)
        if (morningCheckPanel == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var t = canvas.transform.Find("MorningCheckPanel");
                if (t != null) morningCheckPanel = t.gameObject;
            }
        }
        if (startShopBtn == null && morningCheckPanel != null)
        {
            var btnT = morningCheckPanel.transform.Find("Btn_StartShop");
            if (btnT != null) startShopBtn = btnT.GetComponent<Button>();
        }

        if (startShopBtn != null)
        {
            startShopBtn.onClick.RemoveAllListeners();
            startShopBtn.onClick.AddListener(OnStartShopClicked);
        }
    }

    private IEnumerator Start()
    {
        dayCount = KiKs.Combat.RuntimeGameRepository.CurrentDay;
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
        dayCount = KiKs.Combat.RuntimeGameRepository.CurrentDay;
        CurrentPhase = DayPhase.Night;
        EmitPhaseChanged();
        GameEvent.Emit("DayEnded", dayCount);

        if (KiKs.Combat.RuntimeGameRepository.IsFinalCafeDay)
        {
            KiKs.Combat.RuntimeGameRepository.NotifyFinalCafeCompleted();
            GameEvent.Emit("FinalCafeCompleted", dayCount);
            StoryEndingPresenter.Show();
            return;
        }

        if (KiKs.UI.TransitionEffect.Instance != null)
            KiKs.UI.TransitionEffect.Instance.TransitionTo(preBattleSceneName);
        else
            SceneManager.LoadScene(preBattleSceneName);
    }

    public static void EndNightPhaseStatic()
    {
        bool advanced = KiKs.Combat.RuntimeGameRepository.AdvanceDay();
        Debug.Log($"[TimeSystem] EndNightPhaseStatic -> Day {KiKs.Combat.RuntimeGameRepository.CurrentDay}, advanced={advanced}");
        SceneManager.LoadScene("Cafe");
    }


    private void EnterMorningCheck()
    {
        CurrentPhase = DayPhase.MorningCheck;
        Debug.Log($"[TimeSystem] EnterMorningCheck -> Day {dayCount}");
        EmitPhaseChanged();

        if (morningCheckPanel != null)
            morningCheckPanel.SetActive(true);

        if (TrayGridUI.Instance != null)
            TrayGridUI.Instance.ShowSelection();
    }

    public void StartShopPhase()
    {
        if (CurrentPhase == DayPhase.Shop) return; // Guard against double-call
        CurrentPhase = DayPhase.Shop;
        Debug.Log($"[TimeSystem] StartShopPhase -> Day {dayCount}");
        KiKs.Combat.RuntimeGameRepository.ClearCraftedCoffees();
        EmitPhaseChanged();
        GameEvent.Emit("DayStarted", dayCount);

        if (morningCheckPanel != null)
            morningCheckPanel.SetActive(false);

        if (TrayGridUI.Instance != null)
            TrayGridUI.Instance.HideAll();
    }

    private void OnStartShopClicked()
    {
        if (!IngredientTray.HasAny)
        {
            Debug.Log("[TimeSystem] Cannot start shop — no materials selected.");
            return;
        }
        StartShopPhase();
    }

    private void EmitPhaseChanged()
    {
        GameEvent.Emit("PhaseChanged", new PhaseChangedPayload { Phase = CurrentPhase, Day = dayCount });
    }
}
