using KiKs.Audio;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalMouseClickAudio))]
internal sealed class GlobalMouseClickAudioChineseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "该组件会自动安装到全局 AudioManager。进入游戏后，无论当前场景或点击目标是什么，鼠标左键按下都会播放一次。",
            MessageType.Info);

        var cue = serializedObject.FindProperty("clickCue");
        if (cue != null)
        {
            EditorGUILayout.PropertyField(
                cue,
                new GUIContent(
                    "全局左键点击音效",
                    "默认自动加载 Resources/Audio/GlobalMouseLeftClick，无需手动填写。"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
