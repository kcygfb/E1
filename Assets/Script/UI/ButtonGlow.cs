using UnityEngine;
using UnityEngine.UI;

/// <summary>UGUI 按键边缘发光组件。从 sprite alpha 通道生成膨胀+模糊的发光纹理，真正的边缘描边发光。ScreenSpaceOverlay 兼容。仅运行时生成，编辑器不执行。</summary>
[RequireComponent(typeof(Image))]
public class ButtonGlow : MonoBehaviour
{
    [Header("Glow")]
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    [Range(1, 20)]
    [SerializeField] private int glowRadius = 6;
    [Range(1, 4)]
    [SerializeField] private int blurPasses = 2;

    [Header("Pulse")]
    [SerializeField] private bool pulse = true;
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float pulseMin = 0.3f;

    [Header("Control")]
    [SerializeField] private bool isOn = true;

    [Header("Performance")]
    [SerializeField] private int maxTextureSize = 256;
    [SerializeField] private int maxGlowRadiusTex = 16;

    private Image _sourceImage;
    private Image _glowImage;
    private Texture2D _glowTex;
    private Sprite _glowSprite;

    private void OnEnable()
    {
        _sourceImage = GetComponent<Image>();
        CreateOrRefreshGlow();
    }

    private void OnDisable() => DestroyGlow();
    private void OnDestroy() => DestroyGlow();

    private void Update()
    {
        if (_glowImage == null) return;
        _glowImage.enabled = isOn;
        if (!isOn) return;

        float alphaMul = 1f;
        if (pulse)
            alphaMul = Mathf.Lerp(pulseMin, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);

        var c = _glowImage.color;
        _glowImage.color = new Color(c.r, c.g, c.b, alphaMul);
    }

    public void SetOn(bool on) => isOn = on;

    private void CreateOrRefreshGlow()
    {
        if (_sourceImage == null || _sourceImage.sprite == null) return;

        DestroyGlow();

        var parent = transform.parent;
        var glowGO = new GameObject("GlowLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowGO.transform.SetParent(parent, false);
        glowGO.transform.SetSiblingIndex(transform.GetSiblingIndex());

        _glowImage = glowGO.GetComponent<Image>();
        _glowImage.raycastTarget = false;

        var srcRT = _sourceImage.rectTransform;
        var grt = _glowImage.rectTransform;
        grt.anchorMin = srcRT.anchorMin;
        grt.anchorMax = srcRT.anchorMax;
        grt.pivot = srcRT.pivot;
        grt.anchoredPosition = srcRT.anchoredPosition;
        grt.localRotation = srcRT.localRotation;
        grt.localScale = srcRT.localScale;

        var sprite = _sourceImage.sprite;
        var srcTex = sprite.texture;

        // Make a readable copy via RenderTexture (works even if original is not readable)
        var readableTex = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
        var tmpRT = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(srcTex, tmpRT);
        var oldRT = RenderTexture.active;
        RenderTexture.active = tmpRT;
        readableTex.ReadPixels(new Rect(0, 0, srcTex.width, srcTex.height), 0, 0);
        readableTex.Apply();
        RenderTexture.active = oldRT;
        RenderTexture.ReleaseTemporary(tmpRT);

        // Downscale if exceeds maxTextureSize to bound memory and computation
        int sw = readableTex.width;
        int sh = readableTex.height;
        if (sw > maxTextureSize || sh > maxTextureSize)
        {
            float scale = Mathf.Min((float)maxTextureSize / sw, (float)maxTextureSize / sh);
            int newW = Mathf.Max(1, Mathf.CeilToInt(sw * scale));
            int newH = Mathf.Max(1, Mathf.CeilToInt(sh * scale));

            var smallRT = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(readableTex, smallRT);
            var prevRT = RenderTexture.active;
            RenderTexture.active = smallRT;
            var scaledTex = new Texture2D(newW, newH, TextureFormat.RGBA32, false);
            scaledTex.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
            scaledTex.Apply();
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(smallRT);

            Destroy(readableTex);
            readableTex = scaledTex;
            sw = newW;
            sh = newH;
        }

        var srcPixels = readableTex.GetPixels();

        // glowRadius is in screen pixels. Convert to texture pixels using
        // the texel->screen scale so the glow looks consistent at any texture resolution.
        float onScreenX = Mathf.Max(1f, Mathf.Abs(srcRT.rect.width));
        float onScreenY = Mathf.Max(1f, Mathf.Abs(srcRT.rect.height));
        float texelToScreen = Mathf.Max(onScreenX / sw, onScreenY / sh);
        int rTex = Mathf.CeilToInt(glowRadius / texelToScreen);
        rTex = Mathf.Clamp(rTex, 1, maxGlowRadiusTex);

        int pad = rTex + blurPasses + 2;
        int w = sw + pad * 2;
        int h = sh + pad * 2;

        var alpha = new float[w, h];

        // Copy source alpha into padded array
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
                alpha[x + pad, y + pad] = srcPixels[y * sw + x].a;

        // Separable dilation: X pass then Y pass — O(w*h*r) instead of O(w*h*r²)
        var dilatedX = new float[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float maxA = 0f;
                int xMin = Mathf.Max(0, x - rTex);
                int xMax = Mathf.Min(w - 1, x + rTex);
                for (int nx = xMin; nx <= xMax; nx++)
                    if (alpha[nx, y] > maxA) maxA = alpha[nx, y];
                dilatedX[x, y] = maxA;
            }
        }

        var dilated = new float[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float maxA = 0f;
                int yMin = Mathf.Max(0, y - rTex);
                int yMax = Mathf.Min(h - 1, y + rTex);
                for (int ny = yMin; ny <= yMax; ny++)
                    if (dilatedX[x, ny] > maxA) maxA = dilatedX[x, ny];
                dilated[x, y] = maxA;
            }
        }

        // Box blur
        var blurred = dilated;
        for (int pass = 0; pass < blurPasses; pass++)
        {
            var temp = new float[w, h];
            int br = 2 + pass;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    int yMin = Mathf.Max(0, y - br);
                    int yMax = Mathf.Min(h - 1, y + br);
                    int xMin = Mathf.Max(0, x - br);
                    int xMax = Mathf.Min(w - 1, x + br);
                    for (int ny = yMin; ny <= yMax; ny++)
                        for (int nx = xMin; nx <= xMax; nx++)
                        {
                            sum += blurred[nx, ny];
                            count++;
                        }
                    temp[x, y] = sum / count;
                }
            }
            blurred = temp;
        }

        // Build final texture: glow color modulated by blurred alpha
        _glowTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _glowTex.name = sprite.name + "_glow";
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = blurred[x, y];
                // Subtract original alpha so glow only shows on edges
                float origA = alpha[x, y];
                float glowA = Mathf.Clamp01(a - origA);
                int idx = y * w + x;
                pixels[idx] = new Color32(
                    (byte)(glowColor.r * 255),
                    (byte)(glowColor.g * 255),
                    (byte)(glowColor.b * 255),
                    (byte)(glowA * glowColor.a * 255)
                );
            }
        }
        _glowTex.SetPixels32(pixels);
        _glowTex.Apply();

        // Create sprite matching original rect
        _glowSprite = Sprite.Create(_glowTex, new Rect(0, 0, w, h),
            new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
        _glowSprite.name = _glowTex.name;

        _glowImage.sprite = _glowSprite;
        _glowImage.preserveAspect = false;

        // Size: original sizeDelta + padding to account for glow expansion
        var origSize = srcRT.sizeDelta;
        float scaleX = origSize.x / sw;
        float scaleY = origSize.y / sh;
        grt.sizeDelta = new Vector2(w * scaleX, h * scaleY);

        Destroy(readableTex);
    }

    private void DestroyGlow()
    {
        if (_glowImage != null && _glowImage.gameObject != null)
            Destroy(_glowImage.gameObject);
        _glowImage = null;

        if (_glowSprite != null)
        {
            Destroy(_glowSprite);
            _glowSprite = null;
        }
        if (_glowTex != null)
        {
            Destroy(_glowTex);
            _glowTex = null;
        }
    }
}
