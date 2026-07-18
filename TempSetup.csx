using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

var canvas = GameObject.Find("Canvas");

// 1. Create test draw button
var btnGO = new GameObject("Btn_TestDraw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
btnGO.transform.SetParent(canvas.transform, false);
var btnRT = btnGO.GetComponent<RectTransform>();
btnRT.anchorMin = new Vector2(0, 1);
btnRT.anchorMax = new Vector2(0, 1);
btnRT.pivot = new Vector2(0, 1);
btnRT.anchoredPosition = new Vector2(20, -20);
btnRT.sizeDelta = new Vector2(200, 60);
btnGO.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.8f, 1f);

var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
btnTextGO.transform.SetParent(btnGO.transform, false);
var btnTextRT = btnTextGO.GetComponent<RectTransform>();
btnTextRT.anchorMin = Vector2.zero;
btnTextRT.anchorMax = Vector2.one;
btnTextRT.offsetMin = Vector2.zero;
btnTextRT.offsetMax = Vector2.zero;
var btnText = btnTextGO.GetComponent<Text>();
btnText.text = "测试抽牌";
btnText.alignment = TextAnchor.MiddleCenter;
btnText.fontSize = 24;
btnText.color = Color.white;

// 2. Connect Card1's Button to CardView.OnClicked
var card1 = GameObject.Find("Canvas/HandCardArea/Card1");
if (card1 != null)
{
    var cardView = card1.GetComponent<KiKs.Combat.CardView>();
    var button = card1.GetComponent<Button>();
    if (cardView != null && button != null)
    {
        var so = new SerializedObject(button);
        var eventsProp = so.FindProperty("m_OnClick");
        if (eventsProp != null)
        {
            var persistentCalls = eventsProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            persistentCalls.ClearArray();
            
            persistentCalls.InsertArrayElementAtIndex(0);
            var call = persistentCalls.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = cardView;
            call.FindPropertyRelative("m_MethodName").stringValue = "OnClicked";
            call.FindPropertyRelative("m_Mode").intValue = 1;
            call.FindPropertyRelative("m_CallState").intValue = 2;
            
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[SETUP] Card1 Button.onClick -> CardView.OnClicked");
        }
    }
}

var scene = EditorSceneManager.GetActiveScene();
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
Debug.Log("Done!");
