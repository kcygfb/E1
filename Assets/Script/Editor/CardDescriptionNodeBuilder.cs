#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键工具：给卡牌 prefab 添加 DescriptionText 文本节点（CardView 会自动按名字找到它）。
/// 用 PrefabUtility 修改嵌套 prefab，避免手改 YAML 破坏嵌套关系。重复运行会刷新引用，不会重复创建。
/// 先运行「生成卡牌图标SpriteAsset」（图标工具），本工具会把字体和图集引用接上。
/// </summary>
public static class CardDescriptionNodeBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Card_Battle.prefab";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/站酷文艺体 SDF.asset"; // 项目现有动态中文字体（教学框同款）
    private const string SpriteAssetPath = "Assets/TextMesh Pro/Resources/Sprite Assets/CardIcons.asset";
    private const string NodeName = "DescriptionText";

    [MenuItem("Tools/KiKs/Card/添加卡面描述文本节点")]
    public static void Build()
    {
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var root = contents.transform;
            var node = root.Find(NodeName);

            if (node == null)
            {
                var go = new GameObject(NodeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                node = go.transform;
                node.SetParent(root, false);

                // sibling 顺序：CardArt 之后（在 FlashLayer 之前），文字盖在卡图上方
                var cardArt = root.Find("CardArt");
                node.SetSiblingIndex(cardArt != null ? cardArt.GetSiblingIndex() + 1 : root.childCount);

                // 描述区：卡面下部偏上（进游戏后可按美术意见调锚点）
                var rect = (RectTransform)node;
                rect.anchorMin = new Vector2(0.06f, 0.14f);
                rect.anchorMax = new Vector2(0.94f, 0.40f);
                rect.pivot = new Vector2(0.5f, 1.0f);
                rect.offsetMin = rect.offsetMax = rect.sizeDelta = Vector2.zero;
            }

            // 接上字体与图标图集引用（字号/颜色/换行由 CardView 运行时统一设置）
            var text = node.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);

            if (font == null)
                Debug.LogError($"[CardDescriptionNodeBuilder] 找不到中文字体：{FontPath}");
            if (spriteAsset == null)
                Debug.LogWarning($"[CardDescriptionNodeBuilder] 图标图集不存在：{SpriteAssetPath}。请先运行 Tools/KiKs/Card/生成卡牌图标SpriteAsset（把图标 png 放进 Assets/Art/CardIcons/）");

            text.font = font; // Unity 6 TMP 属性名为 font（旧版叫 fontAsset）
            text.spriteAsset = spriteAsset;
            text.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            Debug.Log($"[CardDescriptionNodeBuilder] 完成：{PrefabPath}（font={font?.name}, icons={spriteAsset?.name ?? "未生成"}）");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
#endif
