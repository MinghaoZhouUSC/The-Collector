using UnityEngine;
using UnityEngine.UI;

// 用代码搭 UI 的小工具（uGUI + Unity 内置字体），HUD、背包页面、菜单共用。
// 圆角面板用运行时生成的 9 宫格图片，不需要任何外部素材。
public static class UIBuilder
{
    // ===== 统一配色 =====
    public static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.085f, 0.82f);
    public static readonly Color PanelSolid = new Color(0.035f, 0.055f, 0.085f, 0.97f);
    public static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.12f);
    public static readonly Color LabelColor = new Color(0.55f, 0.58f, 0.66f);   // 小标签
    public static readonly Color SoftText = new Color(0.79f, 0.81f, 0.86f);
    public static readonly Color Accent = new Color(0.45f, 0.9f, 0.7f);         // 安全、达成（和开始界面同色）
    public static readonly Color Gold = new Color(1f, 0.83f, 0.3f);             // 钱
    public static readonly Color Warning = new Color(0.96f, 0.65f, 0.14f);      // 变慢、时间不多
    public static readonly Color Danger = new Color(1f, 0.36f, 0.36f);          // 很慢、时间快到
    public static readonly Color DarkText = new Color(0.07f, 0.09f, 0.13f);     // 彩色底上的深色字

    private const int SpriteSize = 64;
    private const int SpriteRadius = 16;

    private static Font font;
    private static Sprite rounded;

    public static Font DefaultFont => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    // 白色圆角矩形（9 宫格），用 Image.color 染色。
    public static Sprite RoundedSprite => rounded != null ? rounded : (rounded = BuildRoundedSprite());

    // 圆角面板。radius 是屏幕上的圆角半径（按 1920x1080 计）。
    public static Image CreatePanel(Transform parent, string name, Color color, float radius = 14f)
    {
        Image image = CreateImage(parent, name, color);
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        SetRadius(image, radius);
        return image;
    }

    public static void SetRadius(Image image, float radius)
    {
        image.pixelsPerUnitMultiplier = SpriteRadius / Mathf.Max(1f, radius);
    }

    // 小号灰色标签，例如 "TIME LEFT"。
    public static Text CreateLabel(Transform parent, string name, string value, TextAnchor alignment)
    {
        return CreateText(parent, name, value, 20, alignment, LabelColor, false);
    }

    // 圆角进度条：放好底槽，返回填充部分；用 SetFill 设置比例。
    public static Image CreateBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Image track = CreatePanel(parent, name, TrackColor, size.y * 0.5f);
        Place(track.rectTransform, anchor, position, size);

        Image fill = CreatePanel(track.transform, "Fill", Accent, size.y * 0.5f);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return fill;
    }

    public static void SetFill(Image fill, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        fill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        fill.enabled = ratio > 0.001f;
    }

    private static Sprite BuildRoundedSprite()
    {
        var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };

        var pixels = new Color32[SpriteSize * SpriteSize];
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                // 到最近的圆角圆心的距离（不在四个角上时为 0），边缘留 1 像素抗锯齿。
                float cx = Mathf.Clamp(x + 0.5f, SpriteRadius, SpriteSize - SpriteRadius);
                float cy = Mathf.Clamp(y + 0.5f, SpriteRadius, SpriteSize - SpriteRadius);
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                float alpha = Mathf.Clamp01(SpriteRadius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                pixels[y * SpriteSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        var border = new Vector4(SpriteRadius, SpriteRadius, SpriteRadius, SpriteRadius);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    // 屏幕空间的 Canvas，按 1920x1080 缩放。
    public static Canvas CreateScreenCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
    {
        RectTransform rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        image.raycastTarget = false;
        return image;
    }

    public static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Color color, bool shadow = true)
    {
        RectTransform rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        if (shadow)
        {
            var effect = rect.gameObject.AddComponent<Shadow>();
            effect.effectColor = new Color(0f, 0f, 0f, 0.6f);
            effect.effectDistance = new Vector2(2f, -2f);
        }
        return text;
    }

    // 锚点和轴心用同一个点（例如左上角 (0,1)），position 是相对这个点的偏移。
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
