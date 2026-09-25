using UnityEngine;
using UnityEngine.InputSystem;

// 调试面板，挂在 Player 上。默认只在 Editor 里显示，最终版本关闭。
//   + （就是 = 键，不用按 Shift）/ 小键盘 +：加一件测试物品
//   - / 小键盘 -：丢掉最重的一件
//   0 / 小键盘 0：存入全部（模拟回到安全屋）
[RequireComponent(typeof(PlayerEncumbrance))]
public class DebugHUD : MonoBehaviour
{
    [Tooltip("勾选后在 WebGL 等构建版本里也显示。默认只在 Editor 里显示。")]
    [SerializeField] private bool showInBuilds = false;

    [Tooltip("按 + 时加入背包的测试物品")]
    [SerializeField] private ItemData testItem = new ItemData { itemName = "Test", weight = 5f, value = 10 };

    private PlayerInventory inventory;
    private PlayerEncumbrance encumbrance;
    private PlayerState state;
    private Rigidbody2D body;
    private GUIStyle style;

    private void Awake()
    {
        if (!Application.isEditor && !showInBuilds)
        {
            enabled = false;
            return;
        }

        inventory = GetComponent<PlayerInventory>();
        encumbrance = GetComponent<PlayerEncumbrance>();
        state = GetComponent<PlayerState>();
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
        {
            ItemData item = testItem.Clone();
            if (inventory.CanCarry(item)) inventory.Add(item);
            else GameHUD.ShowMessage("Backpack full");
        }

        if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
            inventory.DropHeaviest();

        if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
            inventory.DepositAll();
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8)
            };
        }

        float velocity = body != null ? body.linearVelocity.magnitude : 0f;
        string frozen = state != null ? state.IsFrozen.ToString() : "-";
        string inSafeHouse = state != null ? state.IsInSafeHouse.ToString() : "-";

        string text =
            $"Weight    {inventory.TotalWeight:0.#} / {inventory.Capacity:0.#}   (load {encumbrance.LoadRatio * 100f:0}%)\n" +
            $"Speed     {encumbrance.CurrentSpeed:0.00}   ({encumbrance.SpeedMultiplier * 100f:0}%)   {encumbrance.SpeedLabel}\n" +
            $"Velocity  {velocity:0.00}\n" +
            $"Carrying  ${inventory.TotalValue}    Banked ${inventory.BankedValue}    Key {(inventory.HasKey ? 1 : 0)}/1\n" +
            $"Frozen {frozen}    InSafeHouse {inSafeHouse}\n" +
            "[+] add item   [-] drop heaviest   [0] deposit";

        GUI.Label(new Rect(10f, 10f, 480f, 150f), text, style);
    }
}
