#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// 一键工具：把 Assets/Art/CardIcons/*.png 拼成图集并生成 TMP SpriteAsset。
/// - 图标命名 = 文件名（小写英文），与 CardDescriptionFormatter 的 token 表 value 一致（如 剑 → sword.png）。
/// - 自动内置 missing 占位图标：任何找不到的 `<sprite name="X">` 都会显示 "?" 而不是空白。
/// - 图标按字号的 1.3 倍渲染（faceInfo + scale 烘焙；本 TMP 版本不支持 <sprite scale=> 属性）。
/// 新增图标：放进目录 → 重跑本菜单。
/// </summary>
public static class CardSpriteAssetGenerator
{
    private const string IconDir = "Assets/Art/CardIcons";
    private const string AtlasPath = "Assets/TextMesh Pro/Resources/Sprite Assets/CardIconsAtlas.png";
    private const string OutPath = "Assets/TextMesh Pro/Resources/Sprite Assets/CardIcons.asset";
    private const int CellSize = 256;

    [MenuItem("Tools/KiKs/Card/生成卡牌图标SpriteAsset")]
    public static void Generate()
    {
        if (!Directory.Exists(IconDir)) Directory.CreateDirectory(IconDir);

        var files = AssetDatabase.FindAssets("t:Texture2D", new[] { IconDir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            Debug.LogWarning($"[CardSpriteAssetGenerator] {IconDir} 下没有 png。请放入图标（如 sword.png）后重跑。");
            return;
        }

        var cols = files.Count > 16 ? 8 : 4; // 每格 256：4x4=16 个(1024) 或 8x8=64 个(2048)
        var size = cols * CellSize;
        var atlas = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var rects = new System.Collections.Generic.Dictionary<string, Rect>();

        var index = 0;
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            if (name == "missing") continue; // missing 最后特殊处理（unicode=0）

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
            if (tex == null) continue;

            var cell = new Rect((index % cols) * CellSize, (index / cols) * CellSize, CellSize, CellSize);
            rects[name] = cell;
            BlitIntoCell(tex, atlas, (int)cell.x, (int)cell.y);
            index++;
        }

        // missing 占位：用户提供 missing.png 则用之，否则画灰底 "?"
        var missingFile = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals("missing", StringComparison.OrdinalIgnoreCase));
        var missingCell = new Rect((index % cols) * CellSize, (index / cols) * CellSize, CellSize, CellSize);
        rects["missing"] = missingCell;
        var missingTex = missingFile != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(missingFile) : null;
        if (missingTex != null) BlitIntoCell(missingTex, atlas, (int)missingCell.x, (int)missingCell.y);
        else DrawMissingIcon(atlas, (int)missingCell.x, (int)missingCell.y);

        // 保存图集
        File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var atlasTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        // 建 TMP_SpriteAsset
        if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(OutPath) != null)
            AssetDatabase.DeleteAsset(OutPath);
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        AssetDatabase.CreateAsset(spriteAsset, OutPath);

        // 设置版本号（TMP_Asset.version 的 setter 是 internal，需反射）。
        // 不设置的话 TMP_SpriteAsset.Awake 会认为资源是旧版，在资源导入期间触发
        // UpgradeSpriteAsset()（内部调用 SaveAssets 被 Unity 禁止）而报错。
        typeof(TMP_Asset).GetField("m_Version", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(spriteAsset, "1.1.0");

        spriteAsset.spriteSheet = atlasTex;
        spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);
        // 关键：设基准尺寸 100，图标渲染尺寸 = 字号 × sprite.scale(1.3)
        spriteAsset.faceInfo = new FaceInfo
        {
            pointSize = 100, scale = 1f, lineHeight = 120f,
            ascentLine = 100f, capLine = 100f, meanLine = 100f,
            baseline = 0f, descentLine = 20f,
        };

        uint unicode = 0xFFFE;
        foreach (var kv in rects.OrderBy(kv => kv.Value.y).ThenBy(kv => kv.Value.x))
        {
            var glyph = new TMP_SpriteGlyph
            {
                index = (uint)spriteAsset.spriteGlyphTable.Count,
                metrics = new GlyphMetrics(100f, 100f, 3f, 95f, 100f),
                glyphRect = new GlyphRect((int)kv.Value.x, (int)kv.Value.y, (int)kv.Value.width, (int)kv.Value.height),
                scale = 1f,
                sprite = Sprite.Create(atlasTex, kv.Value, new Vector2(0.5f, 0.5f)),
            };
            var character = new TMP_SpriteCharacter(unicode++, glyph)
            {
                name = kv.Key,
                scale = 1.3f,
            };
            if (kv.Key == "missing") character.unicode = 0; // 缺失查找约定的 unicode

            spriteAsset.spriteGlyphTable.Add(glyph);
            spriteAsset.spriteCharacterTable.Add(character);
        }

        spriteAsset.UpdateLookupTables();

        var mat = new Material(Shader.Find("TextMeshPro/Sprite")) { name = spriteAsset.name + " Material" };
        mat.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
        spriteAsset.material = mat;
        AssetDatabase.AddObjectToAsset(mat, spriteAsset);

        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OutPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[CardSpriteAssetGenerator] 完成：{spriteAsset.spriteCharacterTable.Count} 个图标 → {OutPath}");
    }

    /// <summary>把任意尺寸贴图按比例居中画进 256x256 格（不拉伸变形）。</summary>
    private static void BlitIntoCell(Texture2D source, Texture2D atlas, int cellX, int cellY)
    {
        var rt = RenderTexture.GetTemporary(CellSize, CellSize, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        var aspect = source.width / (float)source.height;
        var scale = aspect > 1f ? new Vector2(1f, 1f / aspect) : new Vector2(aspect, 1f);
        Graphics.Blit(source, rt, scale, (Vector2.one - scale) * 0.5f);

        var tmp = new Texture2D(CellSize, CellSize, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, CellSize, CellSize), 0, 0);
        RenderTexture.active = null;
        atlas.SetPixels32(cellX, cellY, CellSize, CellSize, tmp.GetPixels32());
        atlas.Apply();

        UnityEngine.Object.DestroyImmediate(tmp);
        RenderTexture.ReleaseTemporary(rt);
    }

    /// <summary>程序化画「灰底 + 白色 ?」占位图。</summary>
    private static void DrawMissingIcon(Texture2D atlas, int cellX, int cellY)
    {
        const string pattern = "..######..\n.##....##.\n#........#\n........##\n.......##.\n......##..\n.....##...\n....##....\n..........\n..........\n...####...\n...####...";
        var rows = pattern.Split('\n');
        const int scale = 16;
        var startX = cellX + (CellSize - rows[0].Length * scale) / 2;
        var startY = cellY + (CellSize - rows.Length * scale) / 2;

        for (var y = 24; y < CellSize - 24; y++)
            for (var x = 24; x < CellSize - 24; x++)
                if (atlas.GetPixel(cellX + x, cellY + y).a < 0.01f)
                    atlas.SetPixel(cellX + x, cellY + y, new Color(0.25f, 0.25f, 0.28f, 1f));

        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == '#')
                    for (var dy = 0; dy < scale; dy++)
                        for (var dx = 0; dx < scale; dx++)
                            atlas.SetPixel(startX + c * scale + dx, startY + r * scale + dy, Color.white);
        atlas.Apply();
    }
}
#endif
