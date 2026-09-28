using System;
using System.Collections.Generic;
using UnityEngine;

// 玩家背包：负重、携带价值、钥匙、已存价值。
// "约定接口"部分是和 B 约定的，签名不能改；"新增"部分可以继续扩展。
public class PlayerInventory : MonoBehaviour
{
    [Tooltip("背包容量（硬上限）。放进去会超过它的物品捡不起来。")]
    [SerializeField, Min(0.1f)] private float capacity = 30f;

    private readonly List<ItemData> items = new List<ItemData>();
    private bool hasKey;
    private float totalWeight;
    private int totalValue;
    private int bankedValue;

    // 背包内容或已存价值变化时触发（HUD 用）。
    public event Action Changed;

    // ===== 约定接口 =====

    public float TotalWeight => totalWeight;
    public int TotalValue => totalValue;
    public bool HasKey => hasKey;

    // 装不下时不加入。钥匙不占容量，最多持有一把。
    public void Add(ItemData item)
    {
        if (!CanCarry(item)) return;

        if (item.isKey)
            hasKey = true;
        else
            items.Add(item);

        Recalculate();
        Changed?.Invoke();
    }

    // 把最重的一件丢在脚下。钥匙不在物品列表里，所以不会被丢掉。
    public void DropHeaviest()
    {
        if (items.Count == 0) return;

        int heaviest = 0;
        for (int i = 1; i < items.Count; i++)
            if (items[i].weight > items[heaviest].weight) heaviest = i;

        DropItem(heaviest);
    }

    // 已存价值 += 携带价值，清空背包（钥匙保留）。背包为空时什么都不做，可以每帧调用。
    public void DepositAll()
    {
        if (items.Count == 0) return;

        bankedValue += totalValue;
        items.Clear();
        Recalculate();
        Changed?.Invoke();
    }

    public void ConsumeKey()
    {
        if (!hasKey) return;

        hasKey = false;
        Changed?.Invoke();
    }

    // ===== 新增 =====

    public float Capacity => capacity;
    public int BankedValue => bankedValue;

    // 背包里的物品，按拾取顺序排列（不含钥匙）。
    public IReadOnlyList<ItemData> Items => items;

    // 把第 index 件丢在脚下，丢下的可以再捡。
    public void DropItem(int index)
    {
        if (index < 0 || index >= items.Count) return;

        ItemData dropped = items[index];
        items.RemoveAt(index);
        Recalculate();
        ItemFactory.Spawn(dropped, transform.position);
        Changed?.Invoke();
    }

    public bool CanCarry(ItemData item)
    {
        if (item == null) return false;
        if (item.isKey) return !hasKey;
        // 留一点余量，避免浮点累加误差导致刚好装满时判断失败。
        return totalWeight + item.weight <= capacity + 0.0001f;
    }

    private void Recalculate()
    {
        totalWeight = 0f;
        totalValue = 0;
        foreach (ItemData it in items)
        {
            totalWeight += it.weight;
            totalValue += it.value;
        }
    }
}
