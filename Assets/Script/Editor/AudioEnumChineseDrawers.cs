using KiKs.Audio;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioBus))]
internal sealed class AudioBusChineseDrawer : PropertyDrawer
{
    private static readonly string[] Labels =
    {
        "游戏音效（SFX）",
        "界面音效（UI）"
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, Labels);
    }
}

[CustomPropertyDrawer(typeof(AudioOverflowMode))]
internal sealed class AudioOverflowModeChineseDrawer : PropertyDrawer
{
    private static readonly string[] Labels =
    {
        "忽略新声音",
        "替换最早的声音"
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.enumValueIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, Labels);
    }
}
