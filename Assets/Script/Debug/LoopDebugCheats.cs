#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using KiKs.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Development-only shortcuts for quickly exercising the four-day loop.
/// All state mutations go through RuntimeGameRepository.
/// </summary>
public sealed class LoopDebugCheats : MonoBehaviour
{
    private static readonly string[] EnemyIdsByEncounter =
    {
        "demo_ghost",
        "demo_little_girl",
        "demo_big_eye"
    };

    private static readonly (string Id, int Amount)[] InitialResources =
    {
        ("CocoaPowder", 0),
        ("CoffeeBean", 10),
        ("Milk", 10),
        ("Sugar", 10),
        ("Water", 10),
        ("gold", 0),
        ("claw", 0),
        ("wolffur", 0),
        ("oil", 0),
        ("eye", 0),
        ("fire", 0),
        ("snake", 0),
        ("tentacle", 0)
    };

    private bool showHelp = true;
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<LoopDebugCheats>() != null) return;
        var host = new GameObject(nameof(LoopDebugCheats));
        DontDestroyOnLoad(host);
        host.AddComponent<LoopDebugCheats>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[Key.F1].wasPressedThisFrame) showHelp = !showHelp;
        if (keyboard[Key.F2].wasPressedThisFrame) AddTestGold();
        if (keyboard[Key.F3].wasPressedThisFrame) UnlockEverything();
        if (keyboard[Key.F4].wasPressedThisFrame) HealToFull();
        if (keyboard[Key.F5].wasPressedThisFrame) CompleteSelectedAreaAsVictory();
        if (keyboard[Key.F6].wasPressedThisFrame) CompleteSelectedAreaAsDefeat();
        if (keyboard[Key.F7].wasPressedThisFrame) SkipCafe();
        // F8 is already used by DialoguePlayer to advance/skip dialogue.
        // F9 is already used by TimeSystem to skip the cafe summary flow.
        if (keyboard[Key.F10].wasPressedThisFrame) AdvanceOneDay();
        if (keyboard[Key.F11].wasPressedThisFrame) JumpToFinalDay();
        if (keyboard[Key.F12].wasPressedThisFrame) ResetWholeRun();
    }

    private static void AddTestGold()
    {
        RuntimeGameRepository.AddGold(500);
        var coinText = GameObject.Find("CoinText")?.GetComponent<TMP_Text>();
        if (coinText != null) coinText.text = $"{RuntimeGameRepository.Gold}C";
        Notify($"CHEAT: +500 gold (total {RuntimeGameRepository.Gold})");
    }

    private static void UnlockEverything()
    {
        var definition = LoopProgressionRepository.Definition;
        foreach (var cardId in definition.initiallyHiddenCardIds)
            RuntimeGameRepository.UnlockCard(cardId);
        foreach (var recipeId in definition.initiallyHiddenRecipeIds)
            RuntimeGameRepository.UnlockRecipe(recipeId);

        Notify("CHEAT: all loop cards and recipes unlocked; scene reloaded");
        ReloadActiveScene();
    }

    private static void HealToFull()
    {
        var battle = FindFirstObjectByType<BattleController>();
        if (battle?.State?.Player != null)
        {
            battle.State.Player.RestoreCurrentHealth(battle.State.Player.MaxHealth);
            PlayerGlobalStats.SetHealth(
                battle.State.Player.CurrentHealth,
                battle.State.Player.MaxHealth);
        }
        else
        {
            PlayerGlobalStats.ResetToFull();
        }

        Notify($"CHEAT: health restored to {PlayerGlobalStats.CurrentHealth}");
    }

    private static void CompleteSelectedAreaAsVictory()
    {
        if (!TryGetSelectedPoint(out var point)) return;

        if (point.Type == AreaPointType.Battle)
        {
            if (point.EncounterIndex < 0 || point.EncounterIndex >= EnemyIdsByEncounter.Length)
            {
                Notify("CHEAT failed: selected battle has no valid enemy slot");
                return;
            }

            var enemyId = EnemyIdsByEncounter[point.EncounterIndex];
            var settlementId =
                $"battle:d{RuntimeGameRepository.CurrentDay}:p{DailyAreaMapState.SelectedPointIndex}:{enemyId}";
            try
            {
                var reward = RuntimeGameRepository.ApplyEnemyVictoryReward(enemyId, settlementId);
                Notify(reward.DuplicateSettlement
                    ? "CHEAT: battle reward was already settled"
                    : $"CHEAT: victory reward granted for {enemyId}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Notify("CHEAT failed: " + exception.Message);
                return;
            }
        }

        CompleteAndLoad(defeated: false);
    }

    private static void CompleteSelectedAreaAsDefeat()
    {
        if (!TryGetSelectedPoint(out _)) return;
        Notify("CHEAT: selected area completed as defeat");
        CompleteAndLoad(defeated: true);
    }

    private static bool TryGetSelectedPoint(out DailyAreaMapPoint point)
    {
        point = null;
        if (!DailyAreaMapState.HasSelectedPoint ||
            !DailyAreaMapState.TryGetPoint(DailyAreaMapState.SelectedPointIndex, out point))
        {
            Notify("CHEAT: select a battle or treasure point in PreBattle first");
            return false;
        }

        if (point.Type == AreaPointType.Event)
        {
            Notify("CHEAT: the event point is intentionally disabled");
            return false;
        }

        return true;
    }

    private static void CompleteAndLoad(bool defeated)
    {
        var result = RuntimeGameRepository.CompleteSelectedArea(defeated);
        LoadScene(string.IsNullOrWhiteSpace(result.NextSceneName) ? "PreBattle" : result.NextSceneName);
    }

    private static void SkipCafe()
    {
        if (SceneManager.GetActiveScene().name != "Cafe")
        {
            Notify("CHEAT: F7 only works in Cafe");
            return;
        }

        if (RuntimeGameRepository.IsFinalCafeDay)
        {
            RuntimeGameRepository.NotifyFinalCafeCompleted();
            StoryEndingPresenter.Show();
            Notify("CHEAT: final cafe skipped to story ending");
            return;
        }

        RuntimeGameRepository.ClearCraftedCoffees();
        RuntimeGameRepository.ClearSelectedCoffees();
        Notify("CHEAT: cafe skipped; entering night map");
        LoadScene("PreBattle");
    }

    private static void AdvanceOneDay()
    {
        if (!RuntimeGameRepository.AdvanceDay())
        {
            Notify("CHEAT: already on the final day");
            return;
        }

        Notify($"CHEAT: advanced to day {RuntimeGameRepository.CurrentDay}");
        LoadScene("Cafe");
    }

    private static void JumpToFinalDay()
    {
        while (RuntimeGameRepository.AdvanceDay()) { }
        Notify($"CHEAT: jumped to final day {RuntimeGameRepository.CurrentDay}");
        LoadScene("Cafe");
    }

    private static void ResetWholeRun()
    {
        GameRunLifecycle.ResetForNewGame();
        foreach (var resource in InitialResources)
            SetResourceAmount(resource.Id, resource.Amount);
        Notify("CHEAT: loop progress and dynamic inventory reset");
        LoadScene("Cafe");
    }

    private static void SetResourceAmount(string resourceId, int target)
    {
        var current = RuntimeGameRepository.GetResourceAmount(resourceId);
        if (current < target)
            RuntimeGameRepository.AddResource(resourceId, target - current);
        else if (current > target && !RuntimeGameRepository.SpendResource(resourceId, current - target))
            Debug.LogWarning($"[LoopDebugCheats] Could not reset resource '{resourceId}'.");
    }

    private static void ReloadActiveScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Notify($"CHEAT failed: scene '{sceneName}' is unavailable");
            return;
        }

        if (KiKs.UI.TransitionEffect.Instance != null)
            KiKs.UI.TransitionEffect.Instance.TransitionTo(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private static void Notify(string message)
    {
        Debug.Log("[LoopDebugCheats] " + message);
        KiKs.UI.WarningToast.Show(message);
    }

    private void OnGUI()
    {
        if (!showHelp) return;
        EnsureStyles();
        const float width = 410f;
        const float height = 300f;
        var area = new Rect(12f, 12f, width, height);
        var previousColor = GUI.color;
        GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.92f);
        GUI.Box(area, GUIContent.none, panelStyle);
        GUI.color = previousColor;
        GUILayout.BeginArea(new Rect(26f, 22f, width - 28f, height - 20f));
        GUILayout.Label($"BIG LOOP CHEATS  |  Day {RuntimeGameRepository.CurrentDay}", labelStyle);
        GUILayout.Label("F1   Toggle this help", labelStyle);
        GUILayout.Label("F2   +500 real repository gold", labelStyle);
        GUILayout.Label("F3   Unlock all reward cards/recipes + reload", labelStyle);
        GUILayout.Label("F4   Restore full health", labelStyle);
        GUILayout.Label("F5   Selected node: victory + reward + continue", labelStyle);
        GUILayout.Label("F6   Selected node: defeat + continue", labelStyle);
        GUILayout.Label("F7   Skip Cafe / finish final Cafe", labelStyle);
        GUILayout.Label("F8   Existing dialogue skip", labelStyle);
        GUILayout.Label("F9   Existing Cafe phase skip", labelStyle);
        GUILayout.Label("F10  Advance one day and return Cafe", labelStyle);
        GUILayout.Label("F11  Jump directly to day 4", labelStyle);
        GUILayout.Label("F12  Reset loop + dynamic inventory", labelStyle);
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = Texture2D.whiteTexture;
        panelStyle.normal.textColor = Color.white;
        panelStyle.padding = new RectOffset(12, 12, 10, 10);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };
    }
}
#endif
