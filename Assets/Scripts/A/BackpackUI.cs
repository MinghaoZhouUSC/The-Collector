using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 背包页面，界面由代码生成：B 打开/关闭，↑/↓ 或 W/S 选择，Q 丢掉选中的物品。
// 打开时玩家不能移动（PlayerController 会检查 IsOpen），倒计时照常走；本局结束时自动关闭。
public class BackpackUI : MonoBehaviour
{
    [Header("按键")]
    [SerializeField] private Key toggleKey = Key.B;
    [SerializeField] private Key dropKey = Key.Q;

    [Tooltip("一页最多显示几行，物品更多时列表跟着选中行滚动")]
    [SerializeField, Min(3)] private int visibleRows = 7;

    private const float RowHeight = 62f;
    private const float ListWidth = 720f;
    private static readonly Color RowColor = new Color(0.15f, 0.17f, 0.23f);
    private static readonly Color SelectedColor = new Color(0.23f, 0.27f, 0.39f);
    private static readonly Color Muted = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color Gold = new Color(1f, 0.83f, 0.3f);

    public static bool IsOpen { get; private set; }

    private class Row
    {
        public GameObject root;
        public Image background, accent, icon;
        public Text name, value, weight;
    }

    private readonly List<Row> rows = new List<Row>();
    private PlayerInventory inventory;
    private PlayerState state;
    private GameObject page;
    private Text summaryText, emptyText, rangeText;
    private int selected, firstVisible;

    private void Awake()
    {
        IsOpen = false;
        Build();
        page.SetActive(false);
    }

    private void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[BackpackUI] 场景里没有找到玩家（PlayerInventory）。", this);
            return;
        }

        state = inventory.GetComponent<PlayerState>();
        inventory.Changed += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        IsOpen = false;
        if (inventory != null) inventory.Changed -= OnInventoryChanged;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || inventory == null) return;

        // 本局结束时自动关闭。
        if (IsOpen && !RoundIsPlaying())
        {
            SetOpen(false);
            return;
        }

        if (Pressed(kb, toggleKey))
        {
            if (IsOpen) SetOpen(false);
            else if (CanOpen()) SetOpen(true);
            return;
        }

        if (!IsOpen) return;

        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) MoveSelection(-1);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) MoveSelection(1);
        if (Pressed(kb, dropKey)) DropSelected();
    }

    // 被定住时（陷阱）不能打开背包。
    private bool CanOpen()
    {
        return RoundIsPlaying() && (state == null || !state.IsFrozen);
    }

    private static bool RoundIsPlaying()
    {
        RoundManager round = RoundManager.Instance;
        return round == null || round.State == RoundManager.RoundState.Playing;
    }

    private void SetOpen(bool open)
    {
        IsOpen = open;
        page.SetActive(open);
        if (open) Refresh();
    }

    private void MoveSelection(int delta)
    {
        int count = inventory.Items.Count;
        if (count == 0) return;

        selected = Mathf.Clamp(selected + delta, 0, count - 1);
        Refresh();
    }

    private void DropSelected()
    {
        if (inventory.Items.Count == 0) return;

        string itemName = inventory.Items[selected].itemName;
        inventory.DropItem(selected);
        GameHUD.ShowMessage($"Dropped {itemName}");
    }

    private void OnInventoryChanged()
    {
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        IReadOnlyList<ItemData> items = inventory.Items;
        int count = items.Count;

        summaryText.text = $"Weight {inventory.TotalWeight:0.#} / {inventory.Capacity:0.#}      Value ${inventory.TotalValue}";
        emptyText.gameObject.SetActive(count == 0);

        // 保证选中行在可见范围内。
        selected = count == 0 ? 0 : Mathf.Clamp(selected, 0, count - 1);
        if (selected < firstVisible) firstVisible = selected;
        if (selected >= firstVisible + visibleRows) firstVisible = selected - visibleRows + 1;
        firstVisible = Mathf.Clamp(firstVisible, 0, Mathf.Max(0, count - visibleRows));

        for (int i = 0; i < rows.Count; i++)
        {
            int index = firstVisible + i;
            Row row = rows[i];
            bool visible = index < count;
            row.root.SetActive(visible);
            if (!visible) continue;

            ItemData item = items[index];
            bool isSelected = index == selected;
            row.background.color = isSelected ? SelectedColor : RowColor;
            row.accent.enabled = isSelected;

            // 图标大小跟着物品大小变，一眼能看出轻重。
            float iconSize = Mathf.Lerp(22f, 44f, Mathf.InverseLerp(0.3f, 0.85f, item.scale));
            row.icon.sprite = item.icon;
            row.icon.color = item.color;
            row.icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);

            row.name.text = item.itemName;
            row.value.text = $"${item.value}";
            row.weight.text = item.weight.ToString("0.#");
        }

        rangeText.text = count > visibleRows
            ? $"{firstVisible + 1}-{Mathf.Min(firstVisible + visibleRows, count)} of {count}"
            : "";
    }

    private void Build()
    {
        Canvas canvas = UIBuilder.CreateScreenCanvas("Backpack Canvas", transform, 20);
        page = canvas.gameObject;

        Image dim = UIBuilder.CreateImage(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
        UIBuilder.Stretch(dim.rectTransform);

        float listHeight = visibleRows * RowHeight;
        Image panel = UIBuilder.CreateImage(canvas.transform, "Panel", new Color(0.1f, 0.11f, 0.15f, 0.97f));
        UIBuilder.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ListWidth + 60f, listHeight + 230f));
        Transform p = panel.transform;

        Text title = UIBuilder.CreateText(p, "Title", "BACKPACK", 40, TextAnchor.UpperLeft, Color.white);
        UIBuilder.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -24f), new Vector2(360f, 52f));

        summaryText = UIBuilder.CreateText(p, "Summary", "", 26, TextAnchor.UpperRight, Muted);
        UIBuilder.Place(summaryText.rectTransform, new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(460f, 40f));

        Header(p, "Item", TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(114f, -92f));
        Header(p, "Value", TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-200f, -92f));
        Header(p, "Weight", TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-54f, -92f));

        RectTransform list = UIBuilder.CreateRect("List", p);
        UIBuilder.Place(list, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(ListWidth, listHeight));
        for (int i = 0; i < visibleRows; i++) rows.Add(CreateRow(list, i));

        emptyText = UIBuilder.CreateText(list, "Empty", "Empty", 30, TextAnchor.MiddleCenter, Muted, false);
        UIBuilder.Stretch(emptyText.rectTransform);

        rangeText = UIBuilder.CreateText(p, "Range", "", 20, TextAnchor.MiddleCenter, Muted, false);
        UIBuilder.Place(rangeText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(300f, 26f));

        Text footer = UIBuilder.CreateText(p, "Footer", "[Up/Down] Select      [Q] Drop      [B] Close", 24, TextAnchor.MiddleCenter, Muted, false);
        UIBuilder.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(ListWidth, 36f));
    }

    private static void Header(Transform parent, string label, TextAnchor alignment, Vector2 corner, Vector2 position)
    {
        Text text = UIBuilder.CreateText(parent, label, label, 22, alignment, Muted, false);
        UIBuilder.Place(text.rectTransform, corner, position, new Vector2(200f, 30f));
    }

    private static Row CreateRow(RectTransform list, int index)
    {
        var row = new Row();

        Image background = UIBuilder.CreateImage(list, $"Row {index}", RowColor);
        UIBuilder.Place(background.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -index * RowHeight), new Vector2(ListWidth, RowHeight - 6f));
        row.root = background.gameObject;
        row.background = background;
        Transform r = background.transform;

        row.accent = UIBuilder.CreateImage(r, "Accent", Gold);
        UIBuilder.Place(row.accent.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, RowHeight - 6f));

        row.icon = UIBuilder.CreateImage(r, "Icon", Color.white);
        row.icon.preserveAspect = true;
        UIBuilder.Place(row.icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(40f, 40f));

        row.name = UIBuilder.CreateText(r, "Name", "", 28, TextAnchor.MiddleLeft, Color.white, false);
        UIBuilder.Place(row.name.rectTransform, new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(360f, 40f));

        row.value = UIBuilder.CreateText(r, "Value", "", 28, TextAnchor.MiddleRight, Gold, false);
        UIBuilder.Place(row.value.rectTransform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(140f, 40f));

        row.weight = UIBuilder.CreateText(r, "Weight", "", 28, TextAnchor.MiddleRight, Color.white, false);
        UIBuilder.Place(row.weight.rectTransform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(120f, 40f));

        return row;
    }

    private static bool Pressed(Keyboard kb, Key key)
    {
        return key != Key.None && kb[key].wasPressedThisFrame;
    }
}
