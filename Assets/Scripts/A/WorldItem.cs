using UnityEngine;

// 地上的物品。按 E 拾取；装不下时提示并留在原地。
// 两种用法：由 ItemFactory 生成（生成后调用 Init）；或者直接摆在场景里，在 Inspector 里指定 Definition。
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [Tooltip("直接摆在场景里的物品用这个指定是什么物品；由 ItemFactory 生成的不用填")]
    [SerializeField] private ItemDefinition definition;

    private SpriteRenderer spriteRenderer;
    private bool pickedUp;

    public ItemData Data { get; private set; }

    public bool CanInteract => Data != null && !pickedUp;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        // 由 ItemFactory 生成的物品在这之前已经 Init 过了。
        if (Data == null && definition != null && definition.data != null)
            Init(definition.data.Clone());
    }

    public void Init(ItemData data)
    {
        Data = data;
        if (data == null) return;

        if (data.icon != null) spriteRenderer.sprite = data.icon;
        spriteRenderer.color = data.color;
        transform.localScale = Vector3.one * data.scale;
        gameObject.name = $"Item ({data.itemName})";
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract || !player.TryGetComponent(out PlayerInventory inventory)) return;

        if (!inventory.CanCarry(Data))
        {
            GameHUD.ShowMessage(Data.isKey ? "You already have a key" : "Backpack full");
            return;
        }

        pickedUp = true;
        inventory.Add(Data);
        Destroy(gameObject);
    }
}
