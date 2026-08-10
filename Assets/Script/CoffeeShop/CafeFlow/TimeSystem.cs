using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cafe scene flow controller. 唯一的阶段驱动者。
/// 阶段流水线: StartOfDay → MorningCheck → Shop → EndOfDay → Settlement → Night
/// 每个阶段完成后通过 Notify* 回调推进到下一阶段。
/// </summary>
public class TimeSystem : MonoBehaviour
{
    public int dayCount = 1;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.StartOfDay;

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
            Debug.Log("[TimeSystem] F9 debug skip -> EnterNight");
            EnterNight();
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

        // Final cafe day: still load Cafe scene but skip normal flow
        if (KiKs.Combat.RuntimeGameRepository.IsFinalCafeDay)
        {
            EnterFinalCafeEnding();
            yield break;
        }

        EnterStartOfDay();
    }

    // ==================== 阶段推进 ====================

    /// <summary>阶段 1: 开场对话。StartOfDayController 监听后播对话，完成时回调 NotifyStartOfDayComplete。</summary>
    private void EnterStartOfDay()
    {
        CurrentPhase = DayPhase.StartOfDay;
        Debug.Log($"[TimeSystem] EnterStartOfDay -> Day {dayCount}");
        EmitPhaseChanged();
        var sodc = FindFirstObjectByType<StartOfDayController>();
        if (sodc == null)
        {
            Debug.Log("[TimeSystem] No StartOfDayController, auto-advancing to MorningCheck");
            EnterMorningCheck();
        }
    }

    /// <summary>StartOfDayController 回调：开场对话完成（或无配置），进入选材阶段。</summary>
    public void NotifyStartOfDayComplete()
    {
        if (CurrentPhase != DayPhase.StartOfDay) return;
        EnterMorningCheck();
    }

    /// <summary>阶段 2: 选材。显示面板，等待玩家点开始经营。</summary>
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

    /// <summary>阶段 3: 经营。CustomerQueue 生成顾客，完成后回调 NotifyAllCustomersServed。</summary>
    public void StartShopPhase()
    {
        if (CurrentPhase == DayPhase.Shop) return;
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

    /// <summary>CustomerQueue 回调：所有普通顾客处理完毕，进入收尾阶段。</summary>
    public void NotifyAllCustomersServed()
    {
        if (CurrentPhase != DayPhase.Shop) return;
        EnterEndOfDay();
    }

    /// <summary>阶段 4: 收尾对话。CustomerQueue 生成收尾NPC，完成后回调 NotifyEndOfDayComplete。</summary>
    private void EnterEndOfDay()
    {
        CurrentPhase = DayPhase.EndOfDay;
        Debug.Log($"[TimeSystem] EnterEndOfDay -> Day {dayCount}");
        EmitPhaseChanged();
    }

    /// <summary>CustomerQueue 回调：收尾NPC全部离场（或无配置），进入结算。</summary>
    public void NotifyEndOfDayComplete()
    {
        if (CurrentPhase != DayPhase.EndOfDay) return;
        EnterSettlement();
    }

    /// <summary>阶段 5: 结算。DailyRevenueSummary 显示结算，确认后进入夜晚。</summary>
    private void EnterSettlement()
    {
        CurrentPhase = DayPhase.Settlement;
        Debug.Log($"[TimeSystem] EnterSettlement -> Day {dayCount}");
        EmitPhaseChanged();
    }

    /// <summary>DailyRevenueSummary 回调：结算完成，进入夜晚转场。</summary>
    public void NotifySettlementComplete()
    {
        if (CurrentPhase != DayPhase.Settlement) return;
        EnterNight();
    }

    /// <summary>阶段 6: 夜晚。转场到 PreBattle。</summary>
    private void EnterNight()
    {
        if (isEndingShop) return;
        isEndingShop = true;
        dayCount = KiKs.Combat.RuntimeGameRepository.CurrentDay;
        CurrentPhase = DayPhase.Night;
        EmitPhaseChanged();
        GameEvent.Emit("DayEnded", dayCount);

        if (KiKs.UI.TransitionEffect.Instance != null)
            KiKs.UI.TransitionEffect.Instance.TransitionTo(preBattleSceneName);
        else
            SceneManager.LoadScene(preBattleSceneName);
    }

    // ==================== Final Cafe Ending (队友新增) ====================

    private void EnterFinalCafeEnding()
    {
        isEndingShop = true;

        if (startShopBtn != null)
            startShopBtn.interactable = false;
        if (morningCheckPanel != null)
            morningCheckPanel.SetActive(false);
        if (TrayGridUI.Instance != null)
            TrayGridUI.Instance.HideAll();

        KiKs.Combat.RuntimeGameRepository.NotifyFinalCafeCompleted();
        GameEvent.Emit("FinalCafeCompleted", dayCount);
        StoryEndingPresenter.Show();
        Debug.Log($"[TimeSystem] Entered final Cafe on Day {dayCount}; story ending shown.");
    }

    // ==================== 兼容旧接口 ====================

    /// <summary>EndDayButton 点击：从 Shop 阶段进入 EndOfDay。</summary>
    public void EndShopPhase()
    {
        if (CurrentPhase == DayPhase.Shop)
            EnterEndOfDay();
    }

    /// <summary>DailyRevenueSummary 旧调用兼容：直接进入 Night。</summary>
    public void CompleteEndShopPhase() => EnterNight();

    public static void EndNightPhaseStatic()
    {
        bool advanced = KiKs.Combat.RuntimeGameRepository.AdvanceDay();
        Debug.Log($"[TimeSystem] EndNightPhaseStatic -> Day {KiKs.Combat.RuntimeGameRepository.CurrentDay}, advanced={advanced}");
        SceneManager.LoadScene("Cafe");
    }

    // ==================== 内部 ====================

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
