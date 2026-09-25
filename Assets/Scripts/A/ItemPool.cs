using System.Collections.Generic;
using UnityEngine;

// 带权重的物品池，在 Inspector 里拖入 ItemDefinition 并设置权重。
// 权重越大越容易被抽到，例如 Low 权重 6、Gem 权重 1。
[System.Serializable]
public class ItemPool
{
    [System.Serializable]
    public class Entry
    {
        public ItemDefinition item;
        [Min(0f)] public float weight = 1f;
    }

    public List<Entry> entries = new List<Entry>();

    // 按权重随机抽一件，返回副本。池子里没有有效条目时返回 null。
    public ItemData PickRandom()
    {
        float total = 0f;
        foreach (Entry e in entries)
            if (IsValid(e)) total += e.weight;

        if (total <= 0f) return null;

        float roll = Random.value * total;
        Entry last = null;
        foreach (Entry e in entries)
        {
            if (!IsValid(e)) continue;
            last = e;
            roll -= e.weight;
            if (roll <= 0f) return e.item.data.Clone();
        }

        // 浮点误差兜底：返回最后一个有效条目。
        return last.item.data.Clone();
    }

    // 抽 count 件，可以重复。池子为空时返回空列表。
    public List<ItemData> PickMany(int count)
    {
        var result = new List<ItemData>(Mathf.Max(count, 0));
        for (int i = 0; i < count; i++)
        {
            ItemData picked = PickRandom();
            if (picked != null) result.Add(picked);
        }
        return result;
    }

    private static bool IsValid(Entry e)
    {
        return e != null && e.item != null && e.item.data != null && e.weight > 0f;
    }
}
