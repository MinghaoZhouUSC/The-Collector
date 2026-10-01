using System;
using System.Collections.Generic;
using UnityEngine;

// 玩家背包：负重、携带价值、钥匙、已存价值，以及三合一自动合成。
// "约定接口"部分是和 B 约定的，签名不能改；"新增"部分可以继续扩展。
public class PlayerInventory : MonoBehaviour
{
    // 集齐几件相同物品时自动合成。
    public const int MergeCount = 3;

    [Tooltip("背包容量（硬上限）。放进去会超过它的物品捡不起来。")]
    [SerializeField, Min(0.1f)] private float capacity = 30f;

    private readonly List<ItemData> items = new List<ItemData>();
    private bool hasKey;
    private float totalWeight;
    private int totalValue;
    private int bankedValue;

    // 背包内容或已存价值变化时触发（HUD 用）。
    public event Action Changed;
    // 自动合成时触发：参数是材料和合成出来的物品。
    public event Action<ItemData, ItemData> Merged;
    // 同一种可合成物品凑到差 1 件时触发：参数是这件物品和当前件数。
    public event Action<ItemData, int> MergeProgress;

    // ===== 约定接口 =====

    public float TotalWeight => totalWeight;
    public int TotalValue => totalValue;
    public bool HasKey => hasKey;

    // 装不下时不加入。钥匙不占容量，最多持有一把。
    // 凑齐 3 件相同的可合成物品时，立即合成为 1 件。
    public void Add(ItemData item)
    {
        if (!CanCarry(item)) return;

        ItemData merged = null;
        if (item.isKey)
            hasKey = true;
        else
        {
            items.Add(item);
            merged = MergeIfComplete(item);
        }

        Recalculate();
        Changed?.Invoke();

        if (merged != null)
            Merged?.Invoke(item, merged);
        else if (CanMerge(item) && CountMatching(item) == MergeCount - 1)
            MergeProgress?.Invoke(item, MergeCount - 1);
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

        // 如果捡起这件会触发合成，按合成之后的重量算，避免"合成后能减重却捡不起来"。
        float weightAfter = totalWeight + item.weight;
        if (CanMerge(item) && CountMatching(item) + 1 >= MergeCount)
            weightAfter += item.mergeResult.data.weight - item.weight * MergeCount;

        // 留一点余量，避免浮点累加误差导致刚好装满时判断失败。
        return weightAfter <= capacity + 0.0001f;
    }

    // 这种物品能不能参与合成（钥匙不行；没设置 Merge Result 的也不行）。
    public static bool CanMerge(ItemData item)
    {
        return item != null && !item.isKey && item.mergeResult != null && item.mergeResult.data != null;
    }

    // 背包里和 item 同一种的物品有几件（按名称判断）。
    public int CountMatching(ItemData item)
    {
        int count = 0;
        foreach (ItemData it in items)
            if (it.itemName == item.itemName) count++;
        return count;
    }

    // 返回一件"再捡 1 件就能合成"的物品；没有则返回 null（教学提示用）。
    public ItemData FindAlmostCompleteSet()
    {
        foreach (ItemData it in items)
            if (CanMerge(it) && CountMatching(it) == MergeCount - 1) return it;
        return null;
    }

    // 同一种可合成物品凑齐 3 件时，移除最早拾取的 3 件，换成 1 件合成物品；返回合成出的物品。
    private ItemData MergeIfComplete(ItemData item)
    {
        if (!CanMerge(item) || CountMatching(item) < MergeCount) return null;

        int removed = 0;
        for (int i = 0; i < items.Count && removed < MergeCount;)
        {
            if (items[i].itemName == item.itemName)
            {
                items.RemoveAt(i);
                removed++;
            }
            else
            {
                i++;
            }
        }

        ItemData result = item.mergeResult.data.Clone();
        items.Add(result);
        return result;
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
