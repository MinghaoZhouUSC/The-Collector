using UnityEngine;
using UnityEngine.UI;

// HUD：界面由代码生成，场景里只需要一个挂了这个组件的空物体。四个角都是圆角半透明卡片：
//   左上：倒计时（剩 60 秒变橙，剩 30 秒变红，最后 10 秒跳动）
//   右上：已存价值 / 目标 + 进度条；下一行是携带价值
//   左下：钥匙
//   右下：负重数字、负重条、速度档位
//   下方中间：操作提示；在安全屋里时显示还差多少，存够了显示 "Press F to extract"
//   上方中间（教学提示框下面）：短暂提示（ShowMessage）
public class GameHUD : MonoBehaviour
{
    [Tooltip("钥匙图标（用 Assets/Sprites 里的 Capsule）。不填就只显示文字")]
    [SerializeField] private Sprite keyIcon;
    [Tooltip("提示信息显示多久（秒）")]
    [SerializeField, Min(0.2f)] private float messageSeconds = 1.8f;
    [Tooltip("剩余时间少于这个秒数时，倒计时变红")]
    [SerializeField, Min(0f)] private float warningSeconds = 30f;
    [Tooltip("剩余时间少于这个秒数时，倒计时变橙")]
    [SerializeField, Min(0f)] private float cautionSeconds = 60f;

    private static readonly Color KeyColor = new Color(0.24f, 0.7f, 0.44f);
    private static readonly Color Faded = new Color(1f, 1f, 1f, 0.2f);

    private const int HintNone = -2;
    private const int HintInteract = -1;
    private const float PunchSeconds = 0.25f;

    private static GameHUD instance;

    private PlayerInventory inventory;
    private PlayerEncumbrance encumbrance;
    private PlayerState state;

    private Text timeText, bankedText, goalText, carryingText, keyText, weightText, speedText, hintText, messageText;
    private Image keyImage, goalFill, loadFill, speedChip, hintPill, messageBorder;
    private RectTransform messageRoot;
    private CanvasGroup messageGroup;
    private float messageUntil;
    private float bankedPunchUntil;
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

        instance.SetMessage(text);
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
        inventory.Merged += OnMerged;
        inventory.MergeProgress += OnMergeProgress;
        shownBanked = inventory.BankedValue;
        RefreshInventory();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (inventory == null) return;

        inventory.Changed -= RefreshInventory;
        inventory.Merged -= OnMerged;
        inventory.MergeProgress -= OnMergeProgress;
    }

    private void OnMerged(ItemData material, ItemData result)
    {
        int valueGain = result.value - material.value * PlayerInventory.MergeCount;
        float weightSaved = material.weight * PlayerInventory.MergeCount - result.weight;
        string sign = valueGain >= 0 ? "+" : "-";
        ShowMessage($"AUTO MERGE! {result.itemName}  {sign}${Mathf.Abs(valueGain)} / -{weightSaved:0.#} weight");
    }

    private void OnMergeProgress(ItemData item, int count)
    {
        ShowMessage($"{count}/{PlayerInventory.MergeCount} {item.itemName} - collect 1 more to merge");
    }

    private void Update()
    {
        UpdateTimer();
        UpdateHint();
        UpdateMessage();
        UpdateBankedPunch();
    }

    private void UpdateTimer()
    {
        RoundManager round = RoundManager.Instance;
        if (round == null) return;

        // 最后 10 秒轻微跳动，提醒赶紧回安全屋。
        bool pulsing = round.State == RoundManager.RoundState.Playing && round.TimeRemaining <= 10f;
        float pulse = pulsing ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(Time.time * 6f)) : 1f;
        timeText.rectTransform.localScale = Vector3.one * pulse;

        int seconds = Mathf.CeilToInt(round.TimeRemaining);
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        timeText.color = seconds <= warningSeconds ? UIBuilder.Danger
            : seconds <= cautionSeconds ? UIBuilder.Warning
            : Color.white;
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
        hintPill.gameObject.SetActive(hint != HintNone);
        hintText.color = hint == 0 ? UIBuilder.Accent : UIBuilder.SoftText;
        hintText.text = hint == HintNone ? ""
            : hint == HintInteract ? "<b><color=#FFFFFF>[E]</color></b> Interact        <b><color=#FFFFFF>[B]</color></b> Backpack"
            : hint == 0 ? "<b>Press F to extract</b>"
            : $"Need <b><color=#FFD34D>${hint}</color></b> more to extract";
    }

    private void SetMessage(string text)
    {
        // 先激活再量文字宽度，保证量出来的宽度准确。
        messageRoot.gameObject.SetActive(true);
        messageText.text = text;
        // 提示条的宽度跟着文字长度变。
        messageRoot.sizeDelta = new Vector2(messageText.preferredWidth + 56f, messageRoot.sizeDelta.y);
        messageUntil = Time.time + messageSeconds;
    }

    private void UpdateMessage()
    {
        float alpha = Mathf.Clamp01((messageUntil - Time.time) / 0.3f);
        messageGroup.alpha = alpha;
        messageRoot.gameObject.SetActive(alpha > 0f);
    }

    // 存入时已存金额"弹"一下。
    private void UpdateBankedPunch()
    {
        float t = Mathf.Clamp01((bankedPunchUntil - Time.time) / PunchSeconds);
        bankedText.rectTransform.localScale = Vector3.one * (1f + 0.25f * t);
    }

    private void RefreshInventory()
    {
        // 已存价值增加了，说明刚存入，给个反馈。
        int banked = inventory.BankedValue;
        if (shownBanked >= 0 && banked > shownBanked)
        {
            ShowMessage($"Banked +${banked - shownBanked}");
            bankedPunchUntil = Time.time + PunchSeconds;
        }
        shownBanked = banked;

        RoundManager round = RoundManager.Instance;
        int target = round != null ? round.TargetValue : 0;
        bool reached = target > 0 && banked >= target;
        bankedText.text = $"${banked}";
        goalText.text = target > 0 ? (reached ? "GOAL REACHED" : $"GOAL ${target}") : "";
        goalText.color = reached ? UIBuilder.Accent : UIBuilder.LabelColor;
        UIBuilder.SetFill(goalFill, target > 0 ? (float)banked / target : 0f);
        carryingText.text = $"Carrying  <b><color=#FFFFFF>${inventory.TotalValue}</color></b>";

        keyText.text = inventory.HasKey ? "KEY  1/1" : "KEY  0/1";
        keyText.color = inventory.HasKey ? Color.white : UIBuilder.LabelColor;
        keyImage.color = inventory.HasKey ? KeyColor : Faded;
        weightText.text = $"{inventory.TotalWeight:0.#} / {inventory.Capacity:0.#}";

        if (encumbrance == null) return;

        string label = encumbrance.SpeedLabel;
        Color color = label == "Normal" ? UIBuilder.Accent : label == "Slow" ? UIBuilder.Warning : UIBuilder.Danger;
        speedText.text = label.ToUpperInvariant();
        speedChip.color = color;
        loadFill.color = color;
        UIBuilder.SetFill(loadFill, encumbrance.LoadRatio);
    }

    private void Build()
    {
        Canvas canvas = UIBuilder.CreateScreenCanvas("HUD Canvas", transform, 10);
        Transform root = canvas.transform;

        // 左上：倒计时
        Image timeCard = Card(root, "Time Card", new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(200f, 100f));
        Label(timeCard, "TIME LEFT", new Vector2(0f, 1f), new Vector2(20f, -14f), TextAnchor.UpperLeft);
        timeText = UIBuilder.CreateText(timeCard.transform, "Time", "", 46, TextAnchor.UpperLeft, Color.white);
        timeText.fontStyle = FontStyle.Bold;
        UIBuilder.Place(timeText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -38f), new Vector2(170f, 56f));

        // 右上：已存价值 / 目标、进度条、携带价值
        Image goalCard = Card(root, "Goal Card", new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(420f, 144f));
        Label(goalCard, "BANKED", new Vector2(0f, 1f), new Vector2(20f, -14f), TextAnchor.UpperLeft);
        goalText = Label(goalCard, "", new Vector2(1f, 1f), new Vector2(-20f, -14f), TextAnchor.UpperRight);
        bankedText = UIBuilder.CreateText(goalCard.transform, "Banked", "", 40, TextAnchor.MiddleLeft, UIBuilder.Gold);
        bankedText.fontStyle = FontStyle.Bold;
        UIBuilder.Place(bankedText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -38f), new Vector2(200f, 48f));
        goalFill = UIBuilder.CreateBar(goalCard.transform, "Goal Bar", new Vector2(0f, 1f), new Vector2(20f, -92f), new Vector2(380f, 12f));
        goalFill.color = UIBuilder.Accent;
        carryingText = UIBuilder.CreateText(goalCard.transform, "Carrying", "", 22, TextAnchor.UpperLeft, UIBuilder.SoftText, false);
        UIBuilder.Place(carryingText.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -110f), new Vector2(380f, 28f));

        // 左下：钥匙
        Image keyCard = Card(root, "Key Card", new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(156f, 60f));
        keyImage = UIBuilder.CreateImage(keyCard.transform, "Key Icon", Faded, keyIcon);
        UIBuilder.Place(keyImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(16f, 34f));
        keyImage.enabled = keyIcon != null;
        keyText = UIBuilder.CreateText(keyCard.transform, "Key", "", 22, TextAnchor.MiddleLeft, UIBuilder.LabelColor, false);
        keyText.fontStyle = FontStyle.Bold;
        UIBuilder.Place(keyText.rectTransform, new Vector2(0f, 0.5f), new Vector2(keyIcon != null ? 48f : 20f, 0f), new Vector2(110f, 32f));

        // 右下：负重数字、速度档位、负重条
        Image loadCard = Card(root, "Load Card", new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(420f, 96f));
        Label(loadCard, "LOAD", new Vector2(0f, 1f), new Vector2(20f, -18f), TextAnchor.UpperLeft);
        weightText = UIBuilder.CreateText(loadCard.transform, "Weight", "", 28, TextAnchor.UpperLeft, Color.white, false);
        weightText.fontStyle = FontStyle.Bold;
        UIBuilder.Place(weightText.rectTransform, new Vector2(0f, 1f), new Vector2(84f, -12f), new Vector2(180f, 34f));
        speedChip = UIBuilder.CreatePanel(loadCard.transform, "Speed Chip", UIBuilder.Accent, 16f);
        UIBuilder.Place(speedChip.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -14f), new Vector2(150f, 32f));
        speedText = UIBuilder.CreateText(speedChip.transform, "Speed", "", 20, TextAnchor.MiddleCenter, UIBuilder.DarkText, false);
        speedText.fontStyle = FontStyle.Bold;
        UIBuilder.Stretch(speedText.rectTransform);
        loadFill = UIBuilder.CreateBar(loadCard.transform, "Load Bar", new Vector2(0f, 1f), new Vector2(20f, -60f), new Vector2(380f, 18f));

        // 下方中间：操作提示
        hintPill = UIBuilder.CreatePanel(root, "Hint", new Color(0.035f, 0.055f, 0.085f, 0.7f), 24f);
        UIBuilder.Place(hintPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 48f));
        hintText = UIBuilder.CreateText(hintPill.transform, "Hint Text", "", 24, TextAnchor.MiddleCenter, UIBuilder.SoftText, false);
        hintText.supportRichText = true;
        UIBuilder.Stretch(hintText.rectTransform);

        // 上方中间：提示条（放在教学提示框下面）。外层是彩色边框，里面是深色底。
        messageBorder = UIBuilder.CreatePanel(root, "Message", UIBuilder.Accent, 22f);
        messageRoot = messageBorder.rectTransform;
        UIBuilder.Place(messageRoot, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(400f, 46f));
        messageGroup = messageBorder.gameObject.AddComponent<CanvasGroup>();
        Image messageInner = UIBuilder.CreatePanel(messageBorder.transform, "Inner", UIBuilder.PanelSolid, 20f);
        UIBuilder.Stretch(messageInner.rectTransform);
        messageInner.rectTransform.offsetMin = new Vector2(2f, 2f);
        messageInner.rectTransform.offsetMax = new Vector2(-2f, -2f);
        messageText = UIBuilder.CreateText(messageInner.transform, "Text", "", 24, TextAnchor.MiddleCenter, Color.white, false);
        UIBuilder.Stretch(messageText.rectTransform);
        messageRoot.gameObject.SetActive(false);
    }

    private static Image Card(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Image card = UIBuilder.CreatePanel(parent, name, UIBuilder.PanelColor, 14f);
        UIBuilder.Place(card.rectTransform, anchor, position, size);
        return card;
    }

    private static Text Label(Image card, string value, Vector2 anchor, Vector2 position, TextAnchor alignment)
    {
        Text label = UIBuilder.CreateLabel(card.transform, value.Length > 0 ? value : "Label", value, alignment);
        UIBuilder.Place(label.rectTransform, anchor, position, new Vector2(200f, 24f));
        return label;
    }
}
