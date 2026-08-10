using System;
using System.Collections.Generic;
using KiKs.Audio;
using UnityEngine;

/// <summary>
/// 咖啡店 GameEvent → AudioCue 适配器。它与 BattleAudioPresenter 一样只翻译事件，
/// 所有场景最终仍使用同一个全局 AudioManager。
/// </summary>
[AddComponentMenu("KiKs/Audio/Cafe Audio Presenter")]
public sealed class CafeAudioPresenter : MonoBehaviour
{
    private sealed class CustomSubscription
    {
        public string Channel;
        public Action<object> Handler;
    }

    [Tooltip("咖啡店所有集中音效映射。Create > KiKs > Audio > Cafe Audio Bindings。")]
    [SerializeField] private CafeAudioBindings bindings;
    [Tooltip("调试注册时开启：每次收到咖啡店 GameEvent 都会输出频道名。正式版本建议关闭。")]
    [SerializeField] private bool logReceivedEvents;

    private readonly List<CustomSubscription> _customSubscriptions = new List<CustomSubscription>();

    private void OnEnable()
    {
        if (bindings == null) return;

        GameEvent.On("PhaseChanged", OnPhaseChanged);
        GameEvent.On("DayEnded", OnDayEnded);
        GameEvent.On("CustomerArrived", OnCustomerArrived);
        GameEvent.On("CustomerReadyToOrder", OnCustomerReadyToOrder);
        GameEvent.On("DialogueRequested", OnDialogueRequested);
        GameEvent.On("DialogueEnded", OnDialogueEnded);
        GameEvent.On("OrderCreated", OnOrderCreated);
        GameEvent.On("CoffeeServed", OnCoffeeServed);
        GameEvent.On("OrderCompleted", OnOrderCompleted);
        GameEvent.On("RevenueAwarded", OnRevenueAwarded);

        RegisterAdditionalEvents();
    }

    private void Start()
    {
        if (bindings == null)
        {
            Debug.LogWarning(
                "[CafeAudioPresenter] Cafe Audio Bindings is empty. " +
                "Create one through Create > KiKs > Audio > Cafe Audio Bindings.", this);
        }
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        GameEvent.Off("DayEnded", OnDayEnded);
        GameEvent.Off("CustomerArrived", OnCustomerArrived);
        GameEvent.Off("CustomerReadyToOrder", OnCustomerReadyToOrder);
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
        GameEvent.Off("DialogueEnded", OnDialogueEnded);
        GameEvent.Off("OrderCreated", OnOrderCreated);
        GameEvent.Off("CoffeeServed", OnCoffeeServed);
        GameEvent.Off("OrderCompleted", OnOrderCompleted);
        GameEvent.Off("RevenueAwarded", OnRevenueAwarded);

        for (var i = 0; i < _customSubscriptions.Count; i++)
        {
            var subscription = _customSubscriptions[i];
            GameEvent.Off(subscription.Channel, subscription.Handler);
        }
        _customSubscriptions.Clear();
    }

    private void OnPhaseChanged(object payload)
    {
        Log("PhaseChanged");
        if (payload is PhaseChangedPayload phase)
            Play(bindings.ResolvePhase(phase.Phase));
    }

    private void OnDayEnded(object payload)
    {
        Log("DayEnded");
        Play(bindings.dayEnded);
    }

    private void OnCustomerArrived(object payload)
    {
        Log("CustomerArrived");
        Play(bindings.customerArrived);
    }

    private void OnCustomerReadyToOrder(object payload)
    {
        Log("CustomerReadyToOrder");
        Play(bindings.customerReadyToOrder);
    }

    private void OnDialogueRequested(object payload)
    {
        Log("DialogueRequested");
        if (payload is DialogueRequest request && request.Context == "wrong_coffee")
        {
            Play(bindings.wrongCoffee != null ? bindings.wrongCoffee : bindings.dialogueOpened);
            return;
        }
        Play(bindings.dialogueOpened);
    }

    private void OnDialogueEnded(object payload)
    {
        Log("DialogueEnded");
        Play(bindings.dialogueEnded);
    }

    private void OnOrderCreated(object payload)
    {
        Log("OrderCreated");
        Play(bindings.orderCreated);
    }

    private void OnCoffeeServed(object payload)
    {
        Log("CoffeeServed");
        Play(bindings.coffeeServed);
    }

    private void OnOrderCompleted(object payload)
    {
        Log("OrderCompleted");
        Play(bindings.orderCompleted);
    }

    private void OnRevenueAwarded(object payload)
    {
        Log("RevenueAwarded");
        Play(bindings.revenueAwarded);
        if (payload is RevenueAwardedPayload revenue && revenue.IsPerfect)
            Play(bindings.perfectOrderBonus);
    }

    private void RegisterAdditionalEvents()
    {
        if (bindings.additionalEvents == null) return;

        for (var i = 0; i < bindings.additionalEvents.Count; i++)
        {
            var entry = bindings.additionalEvents[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.channel) || entry.cue == null)
                continue;

            var captured = entry;
            Action<object> handler = _ =>
            {
                Log(captured.channel);
                AudioManager.TryPlay(captured.cue, captured.volumeScale);
            };

            GameEvent.On(captured.channel, handler);
            _customSubscriptions.Add(new CustomSubscription
            {
                Channel = captured.channel,
                Handler = handler
            });
        }
    }

    private static void Play(AudioCue cue)
    {
        AudioManager.TryPlay(cue);
    }

    private void Log(string channel)
    {
        if (logReceivedEvents)
            Debug.Log("[CafeAudioPresenter] " + channel, this);
    }
}
