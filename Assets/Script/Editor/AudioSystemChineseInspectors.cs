using KiKs.Audio;
using KiKs.Combat;
using UnityEditor;
using UnityEngine;

internal abstract class ChineseAudioEditor : Editor
{
    protected void Begin() => serializedObject.Update();
    protected void End() => serializedObject.ApplyModifiedProperties();

    protected static void Section(string title)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    protected void Field(string propertyName, string label, string tooltip = null, bool children = false)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip ?? string.Empty), children);
    }

    protected static void RelativeField(SerializedProperty parent, string propertyName, string label, string tooltip = null)
    {
        var property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip ?? string.Empty));
    }

    protected static void Help(string text) => EditorGUILayout.HelpBox(text, MessageType.Info);
}

[CustomEditor(typeof(AudioCue))]
internal sealed class AudioCueChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Section("音效注册");
        Field("displayName", "显示名称", "便于查找和阅读，不参与程序匹配。");
        DrawClips();
        Help("可放多个音频文件。每次播放随机选择一个；开启“避免连续重复”后不会连续两次选中同一文件。");

        Section("混音与音量");
        Field("bus", "音量分类", "战斗和制作选游戏音效；按钮和菜单选界面音效。");
        Field("output", "输出混音组（可留空）", "可选 AudioMixerGroup；留空仍可正常播放。");
        Field("volume", "自身音量");
        Field("pitchRange", "随机音高范围", "X 为最小值，Y 为最大值；1/1 表示不随机。");
        Field("priority", "播放优先级", "数值越小越重要：0 最高，256 最低。");

        Section("重复播放保护");
        Field("cooldown", "冷却时间（秒）");
        Field("maxSimultaneous", "最大同时播放数");
        Field("overflowMode", "超出并发时");
        Field("avoidImmediateRepeat", "避免连续重复");

        Section("可选的 3D 空间音效");
        Field("spatialBlend", "2D / 3D 混合", "0 为完全 2D，1 为完全 3D。");
        Field("minDistance", "最小距离");
        Field("maxDistance", "最大距离");
        Field("rolloffMode", "距离衰减方式");
        Field("ignoreListenerPause", "忽略全局监听器暂停");
        End();
    }

    private void DrawClips()
    {
        var clips = serializedObject.FindProperty("clips");
        if (clips == null) return;
        var count = Mathf.Max(0, EditorGUILayout.IntField("音频文件数量", clips.arraySize));
        if (count != clips.arraySize) clips.arraySize = count;
        EditorGUI.indentLevel++;
        for (var i = 0; i < clips.arraySize; i++)
            EditorGUILayout.PropertyField(clips.GetArrayElementAtIndex(i), new GUIContent("音频文件 " + (i + 1)));
        EditorGUI.indentLevel--;
    }
}

[CustomEditor(typeof(AudioManager))]
internal sealed class AudioManagerChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("AudioManager 全游戏共用并跨场景保留，通常会在第一次播放时自动创建。");
        Section("播放通道池");
        Field("initialVoices", "启动时预创建通道数");
        Field("maxVoices", "全局最大通道数");
        Section("默认音量");
        Field("defaultMasterVolume", "默认总音量");
        Field("defaultSfxVolume", "默认游戏音效音量");
        Field("defaultUiVolume", "默认界面音效音量");
        Help("PlayerPrefs 已保存过音量时，运行时优先使用保存值。");
        End();
    }
}

[CustomEditor(typeof(AudioCuePlayer))]
internal sealed class AudioCuePlayerChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Section("播放设置");
        Field("cue", "要播放的音效 Cue");
        Field("volumeScale", "额外音量倍率");
        Field("playOnEnable", "对象启用时自动播放");
        Field("playAtTransform", "按对象位置播放 3D 声音");
        Help("UnityEvent 或 Animation Event 可调用 Play()；Stop() 会停止所有正在播放的同一 Cue。");
        End();
    }
}

[CustomEditor(typeof(AudioButtonFeedback))]
internal sealed class AudioButtonFeedbackChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Section("按钮音效");
        Field("hover", "鼠标悬浮音效");
        Field("click", "按钮点击音效");
        End();
    }
}

[CustomEditor(typeof(AudioInteractionFeedback))]
internal sealed class AudioInteractionFeedbackChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Section("鼠标 / 指针交互");
        Field("pointerEnter", "指针进入音效");
        Field("pointerExit", "指针离开音效");
        Field("pointerDown", "指针按下音效");
        Field("pointerUp", "指针松开音效");
        Field("pointerClick", "完成点击音效");
        Section("拖拽交互");
        Field("dragStarted", "开始拖动音效");
        Field("dragReleased", "松开拖拽物音效", "只代表松手，不保证业务上放置成功。");
        Field("receivedDrop", "接收到拖放音效");
        End();
    }
}

[CustomEditor(typeof(BattleAudioBindings))]
internal sealed class BattleAudioBindingsChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("集中决定战斗事件播放哪个 AudioCue。字段留空代表该事件静音。");
        Section("卡牌移动");
        Field("cardDraw", "抽牌音效", "每成功抽取一张牌播放一次。");
        Field("deckReshuffled", "洗牌音效", "弃牌堆洗回抽牌堆时播放一次。");
        Field("cardDiscard", "弃牌音效", "卡牌进入弃牌堆时播放。");
        Section("成功出牌（按卡牌类别）");
        Field("meleeCardPlayed", "近战牌出牌音效");
        Field("rangedCardPlayed", "射击牌出牌音效");
        Field("magicCardPlayed", "魔法牌出牌音效");
        Field("defenseCardPlayed", "防御牌出牌音效");
        Field("fallbackCardPlayed", "通用 / 兜底出牌音效");
        Section("战斗结果");
        Field("playerHit", "玩家受击音效");
        Field("enemyHit", "敌人受击音效");
        Field("enemyKilled", "敌人击杀音效", "击杀敌人时播放一次。");
        Field("toughnessBroken", "破韧音效");
        Field("healing", "治疗音效");
        Field("statusApplied", "施加状态音效");
        Section("战斗结局");
        Field("victory", "胜利音效");
        Field("defeat", "失败音效");
        End();
    }
}

[CustomEditor(typeof(BattleAudioPresenter))]
internal sealed class BattleAudioPresenterChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("只把战斗事件翻译为音效请求；真正播放仍由全局 AudioManager 完成。");
        Field("battleController", "战斗控制器（可留空）", "留空时自动寻找。");
        Field("bindings", "战斗音效映射表");
        End();
    }
}

[CustomEditor(typeof(CafeAudioBindings))]
internal sealed class CafeAudioBindingsChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("集中保存咖啡店事件到 AudioCue 的映射。字段留空代表该事件静音。");
        Section("营业日与店铺流程");
        Field("morningCheckStarted", "进入早晨材料检查音效");
        Field("shopStarted", "正式开店音效");
        Field("nightStarted", "进入夜晚音效");
        Field("dayEnded", "一天结束音效");
        Field("shopReadyToClose", "可以关店音效");
        Section("顾客与对话");
        Field("customerArrived", "顾客到店音效");
        Field("customerReadyToOrder", "顾客准备下单音效");
        Field("dialogueOpened", "打开普通对话音效");
        Field("dialogueEnded", "对话结束音效");
        Field("wrongCoffee", "提交错误咖啡音效");
        Section("订单与收益");
        Field("orderCreated", "订单创建音效");
        Field("coffeeServed", "提交咖啡音效");
        Field("orderCompleted", "正确订单完成音效");
        Field("revenueAwarded", "金币到账音效");
        Field("perfectOrderBonus", "全 Perfect 额外奖励音效");
        Section("QTE 结果");
        Field("qtePerfect", "QTE：Perfect 音效");
        Field("qteGood", "QTE：Good 音效");
        Field("qteMiss", "QTE：Miss 音效");
        DrawAdditionalEvents();
        End();
    }

    private void DrawAdditionalEvents()
    {
        Section("额外 GameEvent 事件");
        Help("用于研磨完成、材料成功放置等未来事件；频道名必须与 GameEvent.Emit 完全一致。");
        var events = serializedObject.FindProperty("additionalEvents");
        if (events == null) return;
        var count = Mathf.Max(0, EditorGUILayout.IntField("额外事件数量", events.arraySize));
        if (count != events.arraySize) events.arraySize = count;
        for (var i = 0; i < events.arraySize; i++)
        {
            var entry = events.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("额外事件 " + (i + 1), EditorStyles.boldLabel);
            RelativeField(entry, "displayName", "中文说明");
            RelativeField(entry, "channel", "事件频道名");
            RelativeField(entry, "cue", "播放的音效 Cue");
            RelativeField(entry, "volumeScale", "额外音量倍率");
            EditorGUILayout.EndVertical();
        }
    }
}

[CustomEditor(typeof(CafeAudioPresenter))]
internal sealed class CafeAudioPresenterChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("监听咖啡店 GameEvent，并调用同一个全局 AudioManager。");
        Field("bindings", "咖啡店音效映射表");
        Field("logReceivedEvents", "在 Console 输出收到的事件");
        End();
    }
}

[CustomEditor(typeof(CafeQteAudioFeedback))]
internal sealed class CafeQteAudioFeedbackChineseEditor : ChineseAudioEditor
{
    public override void OnInspectorGUI()
    {
        Begin();
        Help("挂在 QTE Prefab 根对象上，根据 Perfect / Good / Miss 选择音效。");
        Field("bindings", "咖啡店音效映射表");
        End();
    }
}
