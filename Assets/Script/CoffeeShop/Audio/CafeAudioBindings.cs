using System;
using System.Collections.Generic;
using KiKs.Audio;
using UnityEngine;

/// <summary>
/// 咖啡店音效的集中显式映射表。音效注册人员只需要创建 AudioCue 并拖入对应字段，
/// 不需要读取 AudioManager 或咖啡店业务代码。
/// </summary>
[CreateAssetMenu(fileName = "CafeAudioBindings", menuName = "KiKs/Audio/Cafe Audio Bindings", order = 30)]
public sealed class CafeAudioBindings : ScriptableObject
{
    [Header("Day and shop flow")]
    [Tooltip("进入早晨材料检查阶段。来源：PhaseChanged / MorningCheck。")]
    public AudioCue morningCheckStarted;
    [Tooltip("正式开店。来源：PhaseChanged / Shop。")]
    public AudioCue shopStarted;
    [Tooltip("进入夜晚阶段。来源：PhaseChanged / Night。")]
    public AudioCue nightStarted;
    [Tooltip("一天结束并准备离开咖啡店场景。来源：DayEnded。")]
    public AudioCue dayEnded;
    [Tooltip("所有顾客处理完毕，可以关店。来源：ShopReadyToClose。")]
    public AudioCue shopReadyToClose;

    [Header("Customer and dialogue")]
    [Tooltip("顾客进入咖啡店。来源：CustomerArrived。")]
    public AudioCue customerArrived;
    [Tooltip("顾客准备下单。来源：CustomerReadyToOrder。")]
    public AudioCue customerReadyToOrder;
    [Tooltip("普通对话框打开。来源：DialogueRequested。")]
    public AudioCue dialogueOpened;
    [Tooltip("对话结束。来源：DialogueEnded。")]
    public AudioCue dialogueEnded;
    [Tooltip("提交了错误咖啡时的专用提示；为空时回退到 Dialogue Opened。")]
    public AudioCue wrongCoffee;

    [Header("Order and reward")]
    [Tooltip("订单创建完成并显示订单。来源：OrderCreated。")]
    public AudioCue orderCreated;
    [Tooltip("自由选择模式中选择并提交咖啡。来源：CoffeeServed。")]
    public AudioCue coffeeServed;
    [Tooltip("正确咖啡交付、订单完成。来源：OrderCompleted。")]
    public AudioCue orderCompleted;
    [Tooltip("订单金币到账。来源：RevenueAwarded。")]
    public AudioCue revenueAwarded;
    [Tooltip("全 Perfect 订单的额外声音，可与 Revenue Awarded 叠加。")]
    public AudioCue perfectOrderBonus;

    [Header("QTE result (used by Cafe QTE Audio Feedback)")]
    public AudioCue qtePerfect;
    public AudioCue qteGood;
    public AudioCue qteMiss;

    [Header("Additional GameEvent channels")]
    [Tooltip("为未来或项目特有的 GameEvent 增加映射。Channel 必须与 GameEvent.Emit 使用的字符串完全一致。")]
    public List<CafeGameEventAudioBinding> additionalEvents = new List<CafeGameEventAudioBinding>();

    public AudioCue ResolvePhase(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.MorningCheck:
                return morningCheckStarted;
            case DayPhase.Shop:
                return shopStarted;
            case DayPhase.Night:
                return nightStarted;
            default:
                return null;
        }
    }

    public AudioCue ResolveQte(QTERating rating)
    {
        switch (rating)
        {
            case QTERating.Perfect:
                return qtePerfect;
            case QTERating.Good:
                return qteGood;
            case QTERating.Miss:
                return qteMiss;
            default:
                return null;
        }
    }
}

[Serializable]
public sealed class CafeGameEventAudioBinding
{
    [Tooltip("只用于 Inspector 阅读，例如“研磨完成”。")]
    public string displayName;
    [Tooltip("必须与 GameEvent.Emit 的频道名完全一致，例如 MachineCompleted。")]
    public string channel;
    public AudioCue cue;
    [Range(0f, 2f)] public float volumeScale = 1f;
}
