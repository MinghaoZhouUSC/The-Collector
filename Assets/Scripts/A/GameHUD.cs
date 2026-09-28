using UnityEngine;
using UnityEngine.UI;

// HUD：界面由代码生成，场景里只需要一个挂了这个组件的空物体。
//   左上：倒计时（快结束时变红）
//   右上：携带价值；下一行是已存价值 / 目标
//   左下：钥匙
//   右下：负重条、重量、速度档位
//   下方中间：操作提示；在安全屋里时显示还差多少，存够了显示 "Press F to extract"
//   上方中间：短暂提示（ShowMessage）
public class GameHUD : MonoBehaviour
{
    [Tooltip("钥匙图标（用 Assets/Sprites 里的 Capsule）。不填就只显示文字")]
    [SerializeField] private Sprite keyIcon;
    [Tooltip("提示信息显示多久（秒）")]
    [SerializeField, Min(0.2f)] private float messageSeconds = 1.8f;
    [Tooltip("剩余时间少于这个秒数时，倒计时变红")]
    [SerializeField, Min(0f)] private float warningSeconds = 30f;

    private static readonly Color NormalColor = new Color(0.49f, 0.85f, 0.57f);
    private static readonly Color SlowColor = new Color(0.96f, 0.65f, 0.14f);
    private static readonly Color VerySlowColor = new Color(1f, 0.36f, 0.36f);
    private static readonly Color Gold = new Color(1f, 0.83f, 0.3f);
    private static readonly Color KeyColor = new Color(0.24f, 0.7f, 0.44f);
    private static readonly Color Faded = new Color(1f, 1f, 1f, 0.2f);
    private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);

    private const int HintNone = -2;
    private const int HintInteract = -1;

    private static GameHUD instance;

    private PlayerInventory inventory;
    private PlayerEncumbrance encumbrance;
    private PlayerState state;

    private Text timeText, carryingText, bankedText, keyText, weightText, speedText, hintText, messageText;
    private Image keyImage, barFill;
    private float messageUntil;
    private int shownSeconds = -1;
    private int shownBanked = -1;
    private int shownHint = int.MinValue;

    public static void ShowMessage(string text)
    {
        if (instance == null)
        {
            Debug.Log($"[HUD] {text}");
            return;
        }

        instance.messageText.text = text;
        instance.messageUntil = Time.time + instance.messageSeconds;
    }

    private void Awake()
    {
        instance = this;
        Build();
    }

    private void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[GameHUD] 场景里没有找到玩家（PlayerInventory）。", this);
            return;
        }

        encumbrance = inventory.GetComponent<PlayerEncumbrance>();
        state = inventory.GetComponent<PlayerState>();
        inventory.Changed += RefreshInventory;
        shownBanked = inventory.BankedValue;
        RefreshInventory();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (inventory != null) inventory.Changed -= RefreshInventory;
    }

    private void Update()
    {
        UpdateTimer();
        UpdateHint();
        UpdateMessage();
    }

    private void UpdateTimer()
    {
        RoundManager round = RoundManager.Instance;
        if (round == null) return;

        int seconds = Mathf.CeilToInt(round.TimeRemaining);
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        timeText.color = seconds <= warningSeconds ? VerySlowColor : Color.white;
    }

    // 用一个整数表示当前该显示哪种提示，变了才重新生成文字，避免每帧都创建字符串。
    private void UpdateHint()
    {
        int hint = HintNone;
        RoundManager round = RoundManager.Instance;
        bool playing = round == null || round.State == RoundManager.RoundState.Playing;
        if (playing && inventory != null)
        {
            if (state != null && state.IsInSafeHouse && round != null)
                hint = Mathf.Max(0, round.TargetValue - inventory.BankedValue);   // 0 = 已存够，其余 = 还差多少
            else
                hint = HintInteract;
        }

        if (hint == shownHint) return;

        shownHint = hint;
        hintText.color = hint == 0 ? Gold : HintColor;
        hintText.text = hint == HintNone ? ""
            : hint == HintInteract ? "[E] Interact      [B] Backpack"
            : hint == 0 ? "Press F to extract"
            : $"Need ${hint} more to extract";
    }

    private void UpdateMessage()
    {
        Color color = messageText.color;
        color.a = Mathf.Clamp01((messageUntil - Time.time) / 0.3f);
        messageText.color = color;
    }

    private void RefreshInventory()
    {
        // 已存价值增加了，说明刚存入，给个反馈。
        int banked = inventory.BankedValue;
        if (shownBanked >= 0 && banked > shownBanked) ShowMessage($"Banked +${banked - shownBanked}");
        shownBanked = banked;

        RoundManager round = RoundManager.Instance;
        carryingText.text = $"Carrying ${inventory.TotalValue}";
        bankedText.text = round != null ? $"Banked ${banked} / ${round.TargetValue}" : $"Banked ${banked}";
        bankedText.color = round != null && banked >= round.TargetValue ? NormalColor : Gold;

        keyText.text = inventory.HasKey ? "Key 1/1" : "Key 0/1";
        keyImage.color = inventory.HasKey ? KeyColor : Faded;
        weightText.text = $"Weight {inventory.TotalWeight:0.#} / {inventory.Capacity:0.#}";

        if (encumbrance == null) return;

        string label = encumbrance.SpeedLabel;
        Color color = label == "Normal" ? NormalColor : label == "Slow" ? SlowColor : VerySlowColor;
        speedText.text = $"Speed: {label}";
        speedText.color = color;
        barFill.color = color;
        barFill.rectTransform.anchorMax = new Vector2(encumbrance.LoadRatio, 1f);
    }

    private void Build()
    {
        Canvas canvas = UIBuilder.CreateScreenCanvas("HUD Canvas", transform, 10);
        Transform root = canvas.transform;

        timeText = UIBuilder.CreateText(root, "Time", "", 48, TextAnchor.UpperLeft, Color.white);
        UIBuilder.Place(timeText.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -24f), new Vector2(320f, 64f));

        carryingText = UIBuilder.CreateText(root, "Carrying", "", 40, TextAnchor.UpperRight, Color.white);
        UIBuilder.Place(carryingText.rectTransform, new Vector2(1f, 1f), new Vector2(-32f, -24f), new Vector2(520f, 56f));

        bankedText = UIBuilder.CreateText(root, "Banked", "", 28, TextAnchor.UpperRight, Gold);
        UIBuilder.Place(bankedText.rectTransform, new Vector2(1f, 1f), new Vector2(-32f, -80f), new Vector2(520f, 40f));

        keyImage = UIBuilder.CreateImage(root, "Key Icon", Faded, keyIcon);
        UIBuilder.Place(keyImage.rectTransform, new Vector2(0f, 0f), new Vector2(36f, 30f), new Vector2(28f, 56f));
        keyImage.enabled = keyIcon != null;

        float keyTextX = keyIcon != null ? 80f : 32f;
        keyText = UIBuilder.CreateText(root, "Key", "", 32, TextAnchor.LowerLeft, Color.white);
        UIBuilder.Place(keyText.rectTransform, new Vector2(0f, 0f), new Vector2(keyTextX, 34f), new Vector2(240f, 44f));

        Image barBackground = UIBuilder.CreateImage(root, "Weight Bar", new Color(0f, 0f, 0f, 0.55f));
        UIBuilder.Place(barBackground.rectTransform, new Vector2(1f, 0f), new Vector2(-32f, 32f), new Vector2(380f, 26f));
        barFill = UIBuilder.CreateImage(barBackground.transform, "Fill", NormalColor);
        RectTransform fill = barFill.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        weightText = UIBuilder.CreateText(root, "Weight", "", 28, TextAnchor.LowerRight, Color.white);
        UIBuilder.Place(weightText.rectTransform, new Vector2(1f, 0f), new Vector2(-32f, 66f), new Vector2(420f, 40f));

        speedText = UIBuilder.CreateText(root, "Speed", "", 34, TextAnchor.LowerRight, NormalColor);
        UIBuilder.Place(speedText.rectTransform, new Vector2(1f, 0f), new Vector2(-32f, 106f), new Vector2(420f, 46f));

        hintText = UIBuilder.CreateText(root, "Hint", "", 30, TextAnchor.LowerCenter, HintColor);
        UIBuilder.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(1000f, 48f));

        messageText = UIBuilder.CreateText(root, "Message", "", 36, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0f));
        UIBuilder.Place(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200f, 56f));
    }
}
